describe('FT-SAP-22 Barcode, product images, PDFs, and printing', () => {
  const missingId = 2147483647

  const getProducts = () => cy.request('/api/products').then(({ status, body }) => {
    expect(status).to.eq(200)
    expect(body).to.be.an('array').and.not.be.empty
    return body
  })

  const expectPdf = (response, filePrefix) => {
    expect(response.status).to.eq(200)
    expect(response.headers['content-type']).to.include('application/pdf')
    expect(response.headers['content-disposition']).to.include(filePrefix)
    expect(response.body.slice(0, 4)).to.eq('%PDF')
  }

  beforeEach(() => {
    cy.loginAs('admin')
  })

  it('generates barcode, QR, and combined PDF labels and resolves the generated barcode', () => {
    getProducts().then((products) => {
      const product = products.find((item) => item.isActive) ?? products[0]
      expect(product.id, 'product id').to.be.a('number')

      cy.request({ url: `/api/products/${product.id}/barcode-pdf?format=barcode`, encoding: 'binary' })
        .then((response) => expectPdf(response, 'Barcode_'))
      cy.request({ url: `/api/products/${product.id}/barcode-pdf?format=qr`, encoding: 'binary' })
        .then((response) => expectPdf(response, 'QR_'))
      cy.request({ url: `/api/products/${product.id}/barcode-pdf?format=both`, encoding: 'binary' })
        .then((response) => expectPdf(response, 'Barcode_'))

      getProducts().then((refreshedProducts) => {
        const refreshed = refreshedProducts.find((item) => item.id === product.id)
        expect(refreshed, 'refreshed product').to.exist
        expect(refreshed.barcode, 'persisted barcode').to.be.a('string').and.not.be.empty

        cy.request(`/api/products/barcode/${encodeURIComponent(refreshed.barcode)}`).then((response) => {
          expect(response.status).to.eq(200)
          expect(response.body.id).to.eq(product.id)
          expect(response.body.name).to.eq(product.name)
        })
      })
    })
  })

  it('rejects invalid label formats and missing barcode or product records', () => {
    getProducts().then(([product]) => {
      cy.request({
        url: `/api/products/${product.id}/barcode-pdf?format=unsupported`,
        failOnStatusCode: false
      }).then((response) => {
        expect(response.status).to.eq(400)
        expect(JSON.stringify(response.body)).to.include('Format must be barcode, qr, or both.')
      })
    })

    cy.request({
      url: `/api/products/${missingId}/barcode-pdf?format=both`,
      failOnStatusCode: false
    }).its('status').should('eq', 404)

    cy.request({
      url: `/api/products/barcode/QA-MISSING-${Date.now()}`,
      failOnStatusCode: false
    }).its('status').should('eq', 404)
  })

  it('rejects a non-image upload without changing the product image', () => {
    getProducts().then(([product]) => {
      const imageBefore = product.imageUrl

      cy.window().then((win) => {
        const form = new win.FormData()
        const invalidFile = new win.File(['this is not an image'], 'qa-invalid.txt', { type: 'text/plain' })
        form.append('file', invalidFile)
        form.append('rowVersion', product.rowVersion)

        return win.fetch(`/api/products/${product.id}/image`, {
          method: 'POST',
          credentials: 'same-origin',
          body: form
        }).then(async (response) => ({
          status: response.status,
          body: await response.json()
        }))
      }).then(({ status, body }) => {
        expect(status).to.eq(400)
        expect(JSON.stringify(body)).to.include('JPEG, PNG, or WebP')
      })

      getProducts().then((refreshedProducts) => {
        const refreshed = refreshedProducts.find((item) => item.id === product.id)
        expect(refreshed.imageUrl, 'image URL after rejected upload').to.eq(imageBefore)
      })
    })
  })

  it('downloads a one-time order PDF and exposes a working print control', () => {
    cy.request('/api/order-slips').then(({ body: slips }) => {
      expect(slips).to.be.an('array').and.not.be.empty
      const slip = slips[0]

      cy.request(`/api/order-slips/${slip.id}/download-pdf`).then((response) => {
        expect(response.status).to.eq(200)
        expect(response.body.token).to.be.a('string').and.not.be.empty
        const token = response.body.token

        cy.request({ url: `/api/download/${token}`, encoding: 'binary' }).then((download) => {
          expect(download.status).to.eq(200)
          expect(download.headers['content-type']).to.include('application/pdf')
          expect(download.body.slice(0, 4)).to.eq('%PDF')
        })

        cy.request({ url: `/api/download/${token}`, failOnStatusCode: false })
          .its('status').should('eq', 404)
      })

      cy.visit(`/admin/order-slips/${slip.id}`, {
        onBeforeLoad(win) {
          cy.stub(win, 'print').as('printWindow')
        }
      })
      cy.get('button[aria-label="Print this order slip"]', { timeout: 20000 })
        .should('be.visible')
        .and('be.enabled')
        .click()
      cy.get('@printWindow').should('have.been.calledOnce')
    })

    cy.request({
      url: `/api/order-slips/${missingId}/download-pdf`,
      failOnStatusCode: false
    }).its('status').should('eq', 404)
  })
})
