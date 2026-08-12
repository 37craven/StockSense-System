describe('FT-SAP-24 Email, document, and live-assistant failures', () => {
  const missingId = 2147483647

  it('rejects an empty quotation without changing the product catalog', () => {
    cy.loginAs('admin')

    cy.request('/api/products').then(({ body: before }) => {
      expect(before).to.be.an('array').and.not.be.empty
      const product = before[0]
      const snapshot = {
        currentStock: product.currentStock,
        imageUrl: product.imageUrl,
        rowVersion: product.rowVersion
      }

      cy.request({
        method: 'POST',
        url: '/api/products/send-quote',
        body: {
          userEmail: 'qa-unroutable@example.invalid',
          productIds: [missingId]
        },
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.eq(400)
        expect(JSON.stringify(response.body)).to.include('No valid products found.')
      })

      cy.request('/api/products').then(({ body: after }) => {
        const refreshed = after.find((item) => item.id === product.id)
        expect(refreshed, 'product after rejected quote').to.exist
        expect({
          currentStock: refreshed.currentStock,
          imageUrl: refreshed.imageUrl,
          rowVersion: refreshed.rowVersion
        }, 'product after rejected quote').to.deep.eq(snapshot)
      })
    })
  })

  it('keeps an order unchanged when email dispatch is requested from an invalid status', () => {
    cy.loginAs('admin')

    cy.request('/api/order-slips').then(({ body: slips }) => {
      expect(slips).to.be.an('array').and.not.be.empty
      const slip = slips.find((item) => item.status !== 'Approved')
      expect(slip, 'a non-approved order slip').to.exist

      cy.request({
        method: 'POST',
        url: `/api/order-slips/${slip.id}/send-to-supplier`,
        body: { rowVersion: slip.rowVersion },
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.eq(400)
        expect(response.body.code).to.eq('INVALID_STATUS')
      })

      cy.request(`/api/order-slips/${slip.id}`).then(({ body: unchanged }) => {
        expect(unchanged.status).to.eq(slip.status)
        expect(unchanged.rowVersion).to.eq(slip.rowVersion)
      })
    })
  })

  it('returns stable document errors for missing slips and expired download tokens', () => {
    cy.loginAs('admin')

    cy.request({
      url: `/api/order-slips/${missingId}/download-pdf`,
      failOnStatusCode: false
    }).then((response) => {
      expect(response.status).to.eq(404)
    })

    cy.request({
      url: `/api/download/qa-expired-${Date.now()}`,
      failOnStatusCode: false
    }).then((response) => {
      expect(response.status).to.eq(404)
      expect(response.body).to.include('Download expired or not found.')
    })
  })

  it('uses the live chatbot and returns a correlated, non-empty answer', () => {
    cy.loginAs('customer')

    cy.request({
      method: 'POST',
      url: '/api/assistance',
      body: {
        message: 'In one short sentence, what can the SapShop assistant help a customer with?',
        history: []
      },
      timeout: 60000
    }).then((response) => {
      expect(response.status).to.eq(200)
      expect(response.headers['x-correlation-id']).to.be.a('string').and.not.be.empty
      expect(response.body.reply).to.be.a('string').and.not.be.empty
    })
  })

  it('rejects malformed assistant requests and denies employee database prompts', () => {
    cy.loginAs('customer')

    cy.request({
      method: 'POST',
      url: '/api/assistance',
      body: { message: '   ', history: [] },
      failOnStatusCode: false
    }).then((response) => {
      expect(response.status).to.eq(400)
      expect(response.body.error).to.eq('Message is required.')
      expect(response.headers['x-correlation-id']).to.be.a('string').and.not.be.empty
    })

    cy.request({
      method: 'POST',
      url: '/api/assistance',
      body: { message: 'Hello', history: [], injectedRole: 'Admin' },
      failOnStatusCode: false
    }).then((response) => {
      expect(response.status).to.eq(400)
      expect(response.body.error).to.eq('Only the message and history fields are accepted.')
    })

    cy.loginAs('employee')
    cy.request({
      method: 'POST',
      url: '/api/assistance',
      body: { message: 'SELECT * FROM Products', history: [] },
      failOnStatusCode: false
    }).then((response) => {
      expect(response.status).to.eq(403)
    })
  })
})
