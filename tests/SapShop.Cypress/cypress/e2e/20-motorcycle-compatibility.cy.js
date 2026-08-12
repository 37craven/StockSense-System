describe('FT-SAP-20 Motorcycle catalog and compatibility', () => {
  const openMotorcycleManager = () => {
    cy.visit('/admin/prebuilts')
    cy.contains('h1', 'Pre-Built Packages', { timeout: 20000 }).should('be.visible')
    cy.contains('button', /^Manage Motorcycles$/).should('be.enabled').click()
    cy.contains('[role="dialog"]:visible', 'Manage Motorcycles', { timeout: 12000 })
      .last()
      .as('motorcycleDialog')
  }

  const addMotorcycle = (brand, model, cc) => {
    cy.get('@motorcycleDialog').find('input[placeholder="Brand"]').clear().type(brand)
    cy.get('@motorcycleDialog').find('input[placeholder="Model"]').clear().type(model)
    cy.get('@motorcycleDialog').find('input[placeholder="Base CC"]').clear().type(String(cc))
    cy.get('@motorcycleDialog')
      .find('input[placeholder="Base CC"]')
      .parent()
      .find('button')
      .should('be.enabled')
      .click()
    cy.get('@motorcycleDialog').should('contain.text', brand).and('contain.text', model)
  }

  const deleteMotorcycle = (brand, model) => {
    cy.get('@motorcycleDialog')
      .find(`button[title="Delete ${brand} ${model}"]`)
      .scrollIntoView()
      .click()
    cy.contains('[role="dialog"]:visible', 'Delete motorcycle permanently?')
      .last()
      .within(() => {
        cy.get('#motorcycle-delete-confirmation').type('DELETE')
        cy.contains('button', /^Delete permanently$/).should('be.enabled').click()
      })
    cy.get('@motorcycleDialog').should('not.contain.text', brand)
  }

  it('allows an Admin to manage motorcycles and denies Customer access', () => {
    cy.loginAs('admin')
    cy.visit('/admin/prebuilts')
    cy.contains('button', /^Manage Motorcycles$/).should('be.visible')

    cy.loginAs('customer')
    cy.visit('/admin/prebuilts', { failOnStatusCode: false })
    cy.location('pathname').should('not.eq', '/admin/prebuilts')
  })

  it('denies Employee access to the Admin motorcycle catalog', () => {
    cy.loginAs('employee')
    cy.visit('/admin/prebuilts', { failOnStatusCode: false })
    cy.location('pathname').should('not.eq', '/admin/prebuilts')
  })

  it('validates missing data and documents the duplicate-motorcycle boundary', () => {
    const stamp = Date.now()
    const brand = `QA Brand ${stamp}`
    const model = `Duplicate ${stamp}`

    cy.loginAs('admin')
    openMotorcycleManager()
    cy.get('@motorcycleDialog').find('input[placeholder="Base CC"]').parent().find('button').click()
    cy.get('@motorcycleDialog').should('contain.text', 'Brand, Model, and Base CC are all required.')

    addMotorcycle(brand, model, 155)
    cy.get('@motorcycleDialog').find('input[placeholder="Brand"]').type(brand)
    cy.get('@motorcycleDialog').find('input[placeholder="Model"]').type(model)
    cy.get('@motorcycleDialog').find('input[placeholder="Base CC"]').type('155')
    cy.get('@motorcycleDialog').find('input[placeholder="Base CC"]').parent().find('button').click()

    cy.request('/api/motorcycles').then(({ body }) => {
      const duplicates = body.filter(
        (motorcycle) => motorcycle.brand === brand && motorcycle.model === model,
      )
      expect(duplicates, 'only one case-identical motorcycle').to.have.length(1)
    })
    deleteMotorcycle(brand, model)
  })

  it('persists a motorcycle, exposes it to package compatibility, and deletes it', () => {
    const stamp = Date.now()
    const brand = `QA Motor ${stamp}`
    const model = `Compatibility ${stamp}`
    const displayName = `${brand} ${model}`

    cy.loginAs('admin')
    openMotorcycleManager()
    addMotorcycle(brand, model, 160)

    cy.reload()
    cy.contains('button', /^Manage Motorcycles$/, { timeout: 20000 }).click()
    cy.contains('[role="dialog"]:visible', 'Manage Motorcycles').last().as('motorcycleDialog')
    cy.get('@motorcycleDialog').should('contain.text', brand).and('contain.text', model)
    cy.get('@motorcycleDialog').contains('button', /^Close$/).click()

    cy.contains('button', /^Create Package$/).click()
    cy.contains('[role="dialog"]:visible', 'Create Pre-Built Package').last().as('packageDialog')
    cy.get('@packageDialog')
      .contains('label', displayName)
      .should('be.visible')
      .find('input[type="checkbox"]')
      .check()
      .should('be.checked')
    cy.get('@packageDialog').should('contain.text', '1 selected')
    cy.get('@packageDialog').contains('button', /^Cancel$/).click()

    cy.contains('button', /^Manage Motorcycles$/).click()
    cy.contains('[role="dialog"]:visible', 'Manage Motorcycles').last().as('motorcycleDialog')
    deleteMotorcycle(brand, model)
    cy.request('/api/motorcycles').its('body').should((motorcycles) => {
      expect(
        motorcycles.some((motorcycle) => motorcycle.brand === brand && motorcycle.model === model),
      ).to.eq(false)
    })
  })
})
