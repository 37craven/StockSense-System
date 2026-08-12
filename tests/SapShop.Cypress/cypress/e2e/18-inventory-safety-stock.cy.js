describe('FT-SAP-18 Inventory and safety stock', () => {
  const createdProductIds = []

  const firstSupplier = () => cy.api('GET', '/api/suppliers').then(({ body }) => {
    expect(body, 'at least one supplier is required for product tests').to.be.an('array').and.not.be.empty
    return body[0]
  })

  const createProduct = (overrides = {}) => firstSupplier().then((supplier) => {
    const body = {
      name: `QA Inventory ${Date.now()}-${Cypress._.random(1000, 9999)}`,
      brand: 'QA', category: 'QA Testing', price: 25.50, unitCost: 10,
      initialStock: 7, reorderTarget: 3, supplierId: supplier.id,
      imageUrl: '', isActive: true, ...overrides
    }
    return cy.api('POST', '/api/products', body).then((response) => {
      expect(response.status).to.eq(200)
      createdProductIds.push(response.body.id)
      return response.body
    })
  })

  beforeEach(() => cy.loginAs('admin'))

  afterEach(() => {
    const productIds = createdProductIds.splice(0)
    if (productIds.length) {
      cy.loginAs('admin')
      productIds.forEach((id) => {
        cy.request({ method: 'DELETE', url: `/api/products/${id}`, failOnStatusCode: false })
      })
    }
  })

  it('creates a valid product and persists it after refresh and a new login session', () => {
    createProduct().then((product) => {
      cy.visit('/admin/stock')
      cy.get('input[aria-label="Filter by product name"]').clear().type(product.name)
      cy.contains('tr', product.name).should('contain.text', 'Active').and('contain.text', '7')
      cy.reload()
      cy.get('input[aria-label="Filter by product name"]').clear().type(product.name)
      cy.contains('tr', product.name).should('be.visible')
      cy.clearAllCookies()
      Cypress.session.clearAllSavedSessions()
      cy.loginAs('admin')
      cy.api('GET', '/api/inventory/dashboard').then(({ body }) => {
        expect(body.some((row) => row.productId === product.id && row.currentStock === 7)).to.eq(true)
      })
    })
  })

  it('rejects invalid and duplicate product creation without a partial inventory record', () => {
    cy.request({ method: 'POST', url: '/api/products', body: {}, failOnStatusCode: false })
      .its('status').should('eq', 400)
    createProduct().then((product) => firstSupplier().then((supplier) => {
      cy.request({
        method: 'POST', url: '/api/products', failOnStatusCode: false,
        body: { name: product.name, brand: 'QA', category: 'QA Testing', price: 1,
          initialStock: 1, reorderTarget: 1, supplierId: supplier.id }
      }).then((response) => {
        if (response.body?.id) createdProductIds.push(response.body.id)
        expect(response.status).to.eq(400)
      })
      cy.api('GET', '/api/inventory/dashboard').then(({ body }) => {
        expect(body.filter((row) => row.productName === product.name)).to.have.length(1)
      })
    }))
  })

  it('recalculates one product and validates safety-stock settings, missing records, and stale writes', () => {
    createProduct().then((product) => {
      cy.api('POST', `/api/inventory/recalculate/${product.id}`).its('status').should('eq', 200)
      cy.api('GET', `/api/inventory/products/${product.id}/settings`).then(({ body: settings }) => {
        cy.request({
          method: 'PUT', url: `/api/inventory/products/${product.id}/settings`, failOnStatusCode: false,
          body: { ...settings, defaultLeadTimeDays: 0 }
        }).its('status').should('eq', 400)

        cy.api('PUT', `/api/inventory/products/${product.id}/settings`, {
          ...settings, initialEstimatedWeeklyDemand: 2, defaultLeadTimeDays: 5,
          reviewPeriodDays: 7, bufferDays: 2, serviceLevel: 0.95,
          minimumSafetyStock: 1, maximumSafetyStock: 10, minimumOrderQuantity: 1,
          packageSize: 1, maximumStockLevel: 30, isAutomaticOrderEnabled: false
        }).its('status').should('eq', 200)

        cy.request({
          method: 'PUT', url: `/api/inventory/products/${product.id}/settings`,
          body: settings, failOnStatusCode: false
        }).its('status').should('eq', 409)
      })
      cy.request({ method: 'GET', url: '/api/inventory/products/2147483647/settings', failOnStatusCode: false })
        .its('status').should('eq', 404)
    })
  })

  it('supports selected and all recalculation controls for authorized staff', () => {
    createProduct().then((product) => {
      cy.api('POST', '/api/inventory/recalculate-selected', [product.id]).then(({ body }) => {
        expect(body.requestedCount).to.eq(1)
        expect(body.completedCount).to.eq(1)
      })
      cy.api('POST', '/api/inventory/recalculate-all').its('status').should('eq', 200)
      cy.visit('/admin/stock')
      cy.contains('button', /Recalculate\s+all/i, { timeout: 20000 })
        .should('be.visible')
        .and('be.enabled')
    })
  })

  it('allows employees to read and recalculate but forbids inventory mutations', () => {
    createProduct().then((product) => {
      cy.loginAs('employee')
      cy.api('GET', '/api/inventory/dashboard').its('status').should('eq', 200)
      cy.api('POST', `/api/inventory/recalculate/${product.id}`).its('status').should('eq', 200)
      cy.request({ method: 'POST', url: '/api/products', body: {}, failOnStatusCode: false })
        .its('status').should('eq', 403)
      cy.request({
        method: 'PUT', url: `/api/inventory/products/${product.id}/settings`,
        body: { productId: product.id }, failOnStatusCode: false
      }).its('status').should('eq', 403)
    })
  })

  it('forbids customers and anonymous callers from inventory APIs', () => {
    cy.loginAs('customer')
    cy.request({ method: 'GET', url: '/api/inventory/dashboard', failOnStatusCode: false })
      .its('status').should('eq', 403)
    cy.clearAllCookies()
    cy.clearAllLocalStorage()
    cy.clearAllSessionStorage()
    cy.request({ method: 'GET', url: '/api/inventory/dashboard', failOnStatusCode: false })
      .its('status').should('eq', 401)
  })

  it('returns not found for deleting a missing product', () => {
    cy.request({ method: 'DELETE', url: '/api/products/2147483647', failOnStatusCode: false })
      .its('status').should('eq', 404)
  })
})
