describe('FT-SAP-19 Services and mechanics', () => {
  const mechanicIds = []

  beforeEach(() => cy.loginAs('admin'))

  afterEach(() => {
    mechanicIds.splice(0).forEach((id) => {
      cy.request({ method: 'DELETE', url: `/api/mechanics/${id}`, failOnStatusCode: false })
    })
  })

  const createMechanic = (name = `QA Mechanic ${Date.now()}-${Cypress._.random(1000, 9999)}`) =>
    cy.api('POST', '/api/mechanics', { id: 0, name, isActive: true }).then(({ body }) => {
      mechanicIds.push(body.id)
      return body
    })

  it('creates a service, assigns inventory products, and persists the update', () => {
    const name = `QA Service ${Date.now()}-${Cypress._.random(1000, 9999)}`
    cy.api('GET', '/api/services/inventory').then(({ body: products }) => {
      expect(products, 'service inventory').to.be.an('array').and.not.be.empty
      cy.api('POST', '/api/services', { name, price: 100, category: 'QA Testing', estimatedMinutes: 30 })
        .its('status').should('eq', 200)
      cy.api('GET', '/api/services').then(({ body: services }) => {
        const service = services.find((item) => item.name === name)
        expect(service, 'created service').to.exist
        cy.api('POST', '/api/services/update-products', {
          serviceId: service.id, price: 125, productIds: [products[0].id]
        }).its('status').should('eq', 200)
        cy.reload()
        cy.api('GET', '/api/services').then(({ body: refreshed }) => {
          const persisted = refreshed.find((item) => item.id === service.id)
          expect(persisted.price).to.eq(125)
          expect(persisted.requiredProducts.map((item) => item.id)).to.include(products[0].id)
        })
      })
    })
  })

  it('rejects invalid, duplicate, and missing-record service operations', () => {
    const name = `QA Duplicate Service ${Date.now()}`
    cy.api('POST', '/api/services', { name, price: 10, category: 'QA', estimatedMinutes: 10 })
    cy.request({ method: 'POST', url: '/api/services', body: { name: '', price: -1, category: '', estimatedMinutes: 0 }, failOnStatusCode: false })
      .its('status').should('eq', 400)
    cy.request({ method: 'POST', url: '/api/services', body: { name, price: 10, category: 'QA', estimatedMinutes: 10 }, failOnStatusCode: false })
      .its('status').should('eq', 400)
    cy.request({
      method: 'POST', url: '/api/services/update-products', failOnStatusCode: false,
      body: { serviceId: 2147483647, price: 10, productIds: [] }
    }).its('status').should('eq', 404)
  })

  it('shows the service-management controls on the hosted UI', () => {
    cy.visit('/admin/services')
    cy.contains('h1', 'Service Management').should('be.visible')
    cy.contains('button', /^Create Service$/).should('be.enabled')
    cy.get('#service-search').should('be.visible')
    cy.contains('button', /^Manage Service$/).should('exist')
  })

  it('forbids employee and customer service mutations while permitting authenticated reads', () => {
    ;['employee', 'customer'].forEach((role) => {
      cy.loginAs(role)
      cy.api('GET', '/api/services').its('status').should('eq', 200)
      cy.request({
        method: 'POST', url: '/api/services/update-products', failOnStatusCode: false,
        body: { serviceId: 2147483647, price: 1, productIds: [] }
      }).its('status').should('eq', 403)
    })
  })

  it('creates, edits, deactivates, refreshes, and deletes a mechanic', () => {
    createMechanic().then((mechanic) => {
      const updatedName = `${mechanic.name} Updated`
      cy.api('PUT', `/api/mechanics/${mechanic.id}`, { ...mechanic, name: updatedName, isActive: false })
        .its('status').should('eq', 200)
      cy.api('GET', '/api/mechanics/all').then(({ body }) => {
        expect(body).to.deep.include({ id: mechanic.id, name: updatedName, isActive: false })
      })
      cy.visit('/admin/management')
      cy.contains('button', /^Shop Mechanics$/).click()
      cy.reload()
      cy.contains('button', /^Shop Mechanics$/).click()
      cy.contains(updatedName).should('be.visible')
      cy.api('DELETE', `/api/mechanics/${mechanic.id}`).its('status').should('eq', 200)
      mechanicIds.splice(mechanicIds.indexOf(mechanic.id), 1)
      cy.api('GET', '/api/mechanics/all').then(({ body }) => {
        expect(body.some((item) => item.id === mechanic.id)).to.eq(false)
      })
    })
  })

  it('rejects invalid, duplicate, and missing-record mechanic operations', () => {
    const name = `QA Duplicate Mechanic ${Date.now()}`
    createMechanic(name).then(() => {
      cy.request({ method: 'POST', url: '/api/mechanics', body: { id: 0, name: '', isActive: true }, failOnStatusCode: false })
        .then((response) => {
          if (response.body?.id) mechanicIds.push(response.body.id)
          expect(response.status).to.eq(400)
        })
      cy.request({ method: 'POST', url: '/api/mechanics', body: { id: 0, name, isActive: true }, failOnStatusCode: false })
        .then((response) => {
          if (response.body?.id) mechanicIds.push(response.body.id)
          expect(response.status).to.eq(400)
        })
    })
    cy.request({ method: 'PUT', url: '/api/mechanics/2147483647', body: { id: 2147483647, name: 'Missing', isActive: true }, failOnStatusCode: false })
      .its('status').should('eq', 404)
    cy.request({ method: 'DELETE', url: '/api/mechanics/2147483647', failOnStatusCode: false })
      .its('status').should('eq', 404)
  })

  it('forbids employee and customer mechanic mutations and anonymous reads', () => {
    ;['employee', 'customer'].forEach((role) => {
      cy.loginAs(role)
      cy.api('GET', '/api/mechanics').its('status').should('eq', 200)
      cy.request({ method: 'POST', url: '/api/mechanics', body: { name: 'Forbidden', isActive: true }, failOnStatusCode: false })
        .its('status').should('eq', 403)
    })
    cy.clearAllCookies()
    cy.request({ method: 'GET', url: '/api/mechanics', failOnStatusCode: false })
      .its('status').should('eq', 401)
  })
})
