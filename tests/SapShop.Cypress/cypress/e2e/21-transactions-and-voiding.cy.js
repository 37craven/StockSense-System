describe('FT-SAP-21 Transactions and voiding', () => {
  const chooseInStockProduct = () => {
    cy.visit('/admin/pos')
    cy.get('.pc-grid', { timeout: 20000 }).should('be.visible')
    cy.get('.pc-grid > div:visible')
      .filter((_, card) => /Stock:\s*[1-9]\d*/.test(card.innerText))
      .first()
      .then(($card) => {
        const productName = $card.find('div[title]').first().text().trim()
        expect(productName, 'selected in-stock product name').not.to.equal('')
        cy.request('/api/products').then(({ body }) => {
          const product = body.find((candidate) => candidate.name === productName)
          expect(product, 'selected product API record').to.exist
          cy.wrap(product).as('productBeforeSale')
        })
        cy.wrap($card).scrollIntoView().click('center', { force: true })
        cy.wrap(productName).as('soldProductName')
      })
  }

  const completeCashSale = () => {
    const marker = `QA VOID LIFECYCLE ${Date.now()}`
    chooseInStockProduct()
    cy.get('@soldProductName').then((productName) => {
      cy.contains('table tbody tr', productName).should('be.visible').within(() => {
        cy.get('input[type="number"]').should('have.value', '1')
      })
    })
    cy.get('#desktop-payment-method').select('Cash').should('have.value', 'Cash')
    cy.get('#desktop-remarks').clear().type(marker).blur().should('have.value', marker)
    cy.contains('main button', /^Confirm Sale/).should('be.enabled').click()
    cy.contains('[role="dialog"]:visible', 'Confirm sale').last().within(() => {
      cy.contains('button', /^Record sale/).should('be.enabled').click()
    })
    cy.get('.order-summary-modal', { timeout: 20000 })
      .should('be.visible')
      .and('contain.text', marker)
      .within(() => cy.contains('button', /^Close$/).click())
    cy.request('/api/transactions').then(({ body }) => {
      const transaction = body.find((candidate) => candidate.remarks === marker)
      expect(transaction, 'recorded sale transaction').to.exist
      cy.wrap(transaction).as('saleTransaction')
    })
  }

  const openTransactionDetails = (invoiceNumber) => {
    cy.visit('/admin/transactions')
    cy.contains('table tbody tr', invoiceNumber, { timeout: 20000 })
      .should('be.visible')
      .click()
    cy.contains('[role="dialog"]:visible', 'Transaction Details')
      .last()
      .should('contain.text', invoiceNumber)
      .as('transactionDialog')
  }

  it('enforces transaction read and void permissions for every role', () => {
    cy.loginAs('admin')
    cy.request('/api/transactions').its('status').should('eq', 200)

    cy.loginAs('employee')
    cy.request('/api/transactions').its('status').should('eq', 200)
    cy.request({
      method: 'POST',
      url: '/api/transactions/2147483647/void',
      body: { reason: 'Employee must never be allowed to void.' },
      failOnStatusCode: false,
    }).its('status').should('eq', 403)

    cy.loginAs('customer')
    cy.request({ url: '/api/transactions', failOnStatusCode: false })
      .its('status')
      .should('be.oneOf', [401, 403])
  })

  it('records a sale, rejects invalid voids without changing stock, voids it once, and restores stock', () => {
    cy.loginAs('admin')
    completeCashSale()

    cy.get('@saleTransaction').then((transaction) => {
      expect(transaction.isVoided).to.eq(false)
      expect(transaction.items).to.have.length.greaterThan(0)
      expect(transaction.items[0].stockAfter).to.eq(
        transaction.items[0].stockBefore - transaction.items[0].quantity,
      )

      cy.request({
        method: 'POST',
        url: `/api/transactions/${transaction.id}/void`,
        body: { reason: '   ' },
        failOnStatusCode: false,
      }).its('status').should('eq', 400)
      cy.request({
        method: 'POST',
        url: `/api/transactions/${transaction.id}/void`,
        body: { reason: 'x'.repeat(301) },
        failOnStatusCode: false,
      }).its('status').should('eq', 400)
      cy.request({
        method: 'POST',
        url: '/api/transactions/2147483647/void',
        body: { reason: 'Missing-record boundary case.' },
        failOnStatusCode: false,
      }).its('status').should('eq', 404)

      cy.request('/api/transactions').its('body').should((transactions) => {
        expect(transactions.find((candidate) => candidate.id === transaction.id).isVoided).to.eq(false)
      })
      cy.get('@productBeforeSale').then((product) => {
        cy.request('/api/products').its('body').should((products) => {
          expect(
            products.find((candidate) => candidate.id === product.id).currentStock,
            'invalid void requests leave the completed sale stock unchanged',
          ).to.eq(product.currentStock - 1)
        })
      })

      openTransactionDetails(transaction.invoiceNumber)
      cy.get('@transactionDialog').contains('button', /^Void$/).click()
      cy.contains('[role="dialog"]:visible', 'Void this transaction?').last().within(() => {
        cy.get('#void-reason').type('Automated QA void verifies atomic stock restoration.')
        cy.contains('button', /^Void and restore stock$/).should('be.enabled').click()
      })
      cy.contains('[role="dialog"]:visible', 'Transaction Details')
        .last()
        .should('contain.text', 'This transaction has been voided')

      cy.request('/api/transactions').its('body').should((transactions) => {
        expect(transactions.find((candidate) => candidate.id === transaction.id).isVoided).to.eq(true)
      })
      cy.get('@productBeforeSale').then((product) => {
        cy.request('/api/products').its('body').should((products) => {
          expect(products.find((candidate) => candidate.id === product.id).currentStock).to.eq(product.currentStock)
        })
      })

      cy.request({
        method: 'POST',
        url: `/api/transactions/${transaction.id}/void`,
        body: { reason: 'A duplicate void must be idempotently rejected.' },
        failOnStatusCode: false,
      }).its('status').should('eq', 409)
      cy.reload()
      cy.contains('table tbody tr', transaction.invoiceNumber).should('contain.text', 'Voided')
    })
  })
})
