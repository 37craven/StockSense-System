describe('FT-SAP-25 Concurrency and idempotency', () => {
  const stamp = () => `${Date.now()}-${Cypress._.random(10000, 99999)}`

  const createProduct = (suffix = stamp()) => {
    return cy.request('/api/suppliers').then(({ body: suppliers }) => {
      expect(suppliers, 'an existing supplier is required for isolated product data').not.to.be.empty
      return cy.request('POST', '/api/products', {
        name: `QA-CONCURRENCY-${suffix}`,
        brand: 'QA', category: 'QA Testing', price: 10, unitCost: 5,
        initialStock: 10, reorderTarget: 2, supplierId: suppliers[0].id,
        imageUrl: '/images/default-product.png', isActive: true,
      }).its('body')
    })
  }

  const fetchTwice = (url, method, body) => cy.window().then((win) => Promise.all([
    win.fetch(url, { method, credentials: 'same-origin', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }),
    win.fetch(url, { method, credentials: 'same-origin', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }),
  ]).then((responses) => responses.map((response) => response.status).sort()))

  it('enforces the allowed and forbidden API matrix for Admin, Employee, and Customer', () => {
    const missingId = 2147483647

    cy.loginAs('admin')
    cy.request('/api/products').its('status').should('eq', 200)
    cy.request('/api/inventory/dashboard').its('status').should('eq', 200)
    cy.request('/api/transactions').its('status').should('eq', 200)
    cy.request('/api/order-slips').its('status').should('eq', 200)

    cy.loginAs('employee')
    cy.request('/api/products').its('status').should('eq', 200)
    cy.request('/api/inventory/dashboard').its('status').should('eq', 200)
    cy.request('/api/transactions').its('status').should('eq', 200)
    cy.request('/api/order-slips').its('status').should('eq', 200)
    cy.request({ method: 'DELETE', url: `/api/products/${missingId}`, failOnStatusCode: false })
      .its('status').should('eq', 403)
    cy.request({ method: 'POST', url: `/api/transactions/${missingId}/void`, body: { reason: 'role boundary' }, failOnStatusCode: false })
      .its('status').should('eq', 403)
    cy.request({ method: 'POST', url: `/api/order-slips/${missingId}/approve`, body: { rowVersion: '' }, failOnStatusCode: false })
      .its('status').should('eq', 403)

    cy.loginAs('customer')
    cy.request('/api/products').its('status').should('eq', 200)
    ;['/api/inventory/dashboard', '/api/transactions', '/api/order-slips', '/api/suppliers'].forEach((url) => {
      cy.request({ url, failOnStatusCode: false }).its('status').should('be.oneOf', [401, 403])
    })
    cy.request({ method: 'DELETE', url: `/api/products/${missingId}`, failOnStatusCode: false })
      .its('status').should('be.oneOf', [401, 403])
  })

  it('accepts exactly one concurrent inventory write and rejects a stale row version without partial changes', () => {
    cy.loginAs('admin')
    createProduct().then((product) => {
      const command = {
        id: product.id, price: product.price, stockAdjustment: 2,
        reason: 'QA simultaneous write boundary', productRowVersion: product.rowVersion,
      }
      fetchTwice(`/api/products/${product.id}/inventory-values`, 'PUT', command)
        .should('deep.eq', [200, 409])

      cy.request('/api/products').its('body').then((products) => {
        const current = products.find((candidate) => candidate.id === product.id)
        expect(current.currentStock, 'only one adjustment commits').to.eq(product.currentStock + 2)

        cy.request({
          method: 'PUT', url: `/api/products/${product.id}/inventory-values`,
          body: { ...command, stockAdjustment: 1 }, failOnStatusCode: false,
        }).its('status').should('eq', 409)

        cy.request({
          method: 'PUT', url: `/api/products/${product.id}/inventory-values`,
          body: { id: product.id, price: current.price, stockAdjustment: -9999,
            reason: 'QA invalid operation must be atomic', productRowVersion: current.rowVersion },
          failOnStatusCode: false,
        }).its('status').should('eq', 400)
        cy.request('/api/products').its('body').should((after) => {
          expect(after.find((candidate) => candidate.id === product.id).currentStock).to.eq(current.currentStock)
        })
      })
      cy.request('DELETE', `/api/products/${product.id}`)
    })
  })

  it('prevents duplicate order actions from creating two status transitions', () => {
    cy.loginAs('admin')
    createProduct().then((product) => {
      cy.request('/api/suppliers').its('body').then(() => {
        const marker = `QA duplicate approval ${stamp()}`
        cy.request('POST', '/api/order-slips/manual-draft', {
          supplierId: product.supplierId, reason: marker,
          items: [{ productId: product.id, orderedQuantity: 2 }],
        }).then(({ body: draft }) => {
          const command = { remarks: marker, rowVersion: draft.rowVersion }
          fetchTwice(`/api/order-slips/${draft.id}/approve`, 'POST', command)
            .should((statuses) => {
              expect(statuses.filter((status) => status === 200), 'one approval commits').to.have.length(1)
              expect(statuses.filter((status) => status === 400 || status === 409), 'duplicate is rejected').to.have.length(1)
            })
          cy.request(`/api/order-slips/${draft.id}`).its('body').then((approved) => {
            expect(approved.status).to.eq('Approved')
            cy.request({
              method: 'POST', url: `/api/order-slips/${draft.id}/approve`,
              body: command, failOnStatusCode: false,
            }).its('status').should('be.oneOf', [400, 409])
            cy.request('POST', `/api/order-slips/${draft.id}/cancel`, {
              reason: 'QA cleanup of the isolated concurrency order', rowVersion: approved.rowVersion,
            }).its('status').should('eq', 200)
            cy.request('/api/products').its('body').then((products) => {
              const current = products.find((candidate) => candidate.id === product.id)
              cy.request('PUT', `/api/products/${product.id}/status`, {
                isActive: false, productRowVersion: current.rowVersion,
              }).its('status').should('eq', 200)
            })
          })
        })
      })
    })
  })
})
