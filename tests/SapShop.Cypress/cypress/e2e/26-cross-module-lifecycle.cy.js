describe('FT-SAP-26 Cross-module lifecycle', () => {
  const marker = () => `${Date.now()}-${Cypress._.random(10000, 99999)}`

  const findProduct = (id) => cy.request('/api/products').its('body')
    .then((products) => products.find((candidate) => candidate.id === id))

  it('persists product -> purchase order -> receipt -> POS -> transaction -> stock across refresh and login', () => {
    cy.loginAs('admin')
    const run = marker()
    const name = `QA-CROSS-MODULE-${run}`
    const orderReason = `QA cross-module order ${run}`
    const saleRemarks = `QA cross-module sale ${run}`

    cy.request('/api/suppliers').its('body').then((suppliers) => {
      const supplier = suppliers.find((candidate) => candidate.email && /^[^@]+@[^@]+\.[^@]+$/.test(candidate.email))
      expect(supplier, 'a supplier with a deliverable email is required').to.exist
      cy.request('POST', '/api/products', {
        name, brand: 'QA', category: 'QA Testing', price: 25, unitCost: 10,
        initialStock: 2, reorderTarget: 1, supplierId: supplier.id,
        imageUrl: '/images/default-product.png', isActive: true,
      }).its('body').then((product) => {
        cy.request('POST', '/api/order-slips/manual-draft', {
          supplierId: supplier.id, reason: orderReason,
          items: [{ productId: product.id, orderedQuantity: 3 }],
        }).its('body').then((draft) => {
          cy.request('POST', `/api/order-slips/${draft.id}/approve`, {
            remarks: orderReason, rowVersion: draft.rowVersion,
          }).its('body').then((approved) => {
            cy.request('POST', `/api/order-slips/${draft.id}/send-to-supplier`, {
              remarks: orderReason, rowVersion: approved.rowVersion,
            }).its('body').then((ordered) => {
              expect(ordered.status).to.eq('Ordered')
              cy.request('POST', `/api/order-slips/${draft.id}/receive`, {
                receivedAt: new Date().toISOString(), referenceNumber: `QA-${run}`,
                remarks: orderReason, rowVersion: ordered.rowVersion,
                items: [{ orderSlipItemId: ordered.items[0].id, quantityReceived: 3 }],
              }).its('status').should('eq', 200)
            })
          })
        })

        findProduct(product.id).then((receivedProduct) => {
          expect(receivedProduct.currentStock, 'receipt increases stock').to.eq(5)
          cy.visit('/admin/pos')
          cy.get('input[placeholder="Search products or brands..."]').clear().type(name)
          cy.contains('.pc-grid > div:visible', name, { timeout: 20000 })
            .should('be.visible')
            .click('center', { force: true })
          cy.contains('table tbody tr', name).within(() => cy.get('input[type="number"]').should('have.value', '1'))
          cy.get('#desktop-payment-method').select('Cash')
          cy.get('#desktop-remarks').clear().type(saleRemarks).blur()
          cy.contains('main button', /^Confirm Sale/).should('be.enabled').click()
          cy.contains('[role="dialog"]:visible', 'Confirm sale').last().within(() => {
            cy.contains('button', /^Record sale/).should('be.enabled').click()
          })
          cy.get('.order-summary-modal', { timeout: 20000 }).should('contain.text', saleRemarks)
            .within(() => cy.contains('button', /^Close$/).click())

          cy.request('/api/transactions').its('body').then((transactions) => {
            const sale = transactions.find((candidate) => candidate.remarks === saleRemarks)
            expect(sale, 'POS creates a persisted transaction').to.exist
            expect(sale.items[0].stockBefore).to.eq(5)
            expect(sale.items[0].stockAfter).to.eq(4)

            cy.reload()
            cy.logout()
            cy.loginAs('admin')
            cy.request('/api/transactions').its('body').should((afterLogin) => {
              expect(afterLogin.find((candidate) => candidate.id === sale.id), 'sale persists after login/logout').to.exist
            })
            findProduct(product.id).then((persisted) => {
              expect(persisted.currentStock, 'stock persists after refresh and login/logout').to.eq(4)
              cy.request('POST', `/api/transactions/${sale.id}/void`, {
                reason: 'QA lifecycle cleanup restores the sold unit exactly once.',
              })
              findProduct(product.id).then((afterVoid) => {
                cy.request('PUT', `/api/products/${product.id}/status`, {
                  isActive: false, productRowVersion: afterVoid.rowVersion,
                }).its('status').should('eq', 200)
              })
            })
          })
        })
      })
    })
  })
})
