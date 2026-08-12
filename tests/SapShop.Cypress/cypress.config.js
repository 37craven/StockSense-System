const { defineConfig } = require('cypress')
const { authenticator } = require('otplib')

module.exports = defineConfig({
  reporter: 'junit',
  reporterOptions: {
    mochaFile: 'results/junit-[hash].xml',
    toConsole: true
  },
  video: true,
  screenshotOnRunFailure: true,
  viewportWidth: 1440,
  viewportHeight: 900,
  e2e: {
    baseUrl: 'https://sapshop.motorcycles',
    specPattern: 'cypress/e2e/**/*.cy.js',
    supportFile: 'cypress/support/e2e.js',
    testIsolation: true,
    defaultCommandTimeout: 12000,
    pageLoadTimeout: 60000,
    requestTimeout: 20000,
    responseTimeout: 30000,
    retries: { runMode: 1, openMode: 0 },
    setupNodeEvents(on, config) {
      on('task', {
        totp(secret) {
          if (!secret) throw new Error('A TOTP secret is required.')
          return authenticator.generate(secret.replace(/\s+/g, ''))
        }
      })
      on('before:browser:launch', (browser, launchOptions) => {
        if (browser.family === 'chromium') launchOptions.args.push('--disable-dev-shm-usage')
        return launchOptions
      })
      return config
    }
  }
})
