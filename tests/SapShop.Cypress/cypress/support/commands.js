const roles = {
  admin: { emailKey: 'adminEmail', passwordKey: 'adminPassword', landingPath: '/Dashboard' },
  employee: { emailKey: 'employeeEmail', passwordKey: 'employeePassword', landingPath: '/Dashboard' },
  customer: { emailKey: 'customerEmail', passwordKey: 'customerPassword', landingPath: '/' },
  qaAccount: { emailKey: 'qaAccountEmail', passwordKey: 'qaAccountPassword', landingPath: '/' }
}

const completeTwoFactorChallenge = (role) => {
  cy.location('pathname').then((path) => {
    if (!/\/Account\/LoginWith2fa$/i.test(path)) return

    const secret = Cypress.env(`${role}TotpSecret`)
    const recoveryCode = Cypress.env(`${role}RecoveryCode`)
    if (!secret && !recoveryCode) {
      throw new Error(
        `Role '${role}' requires 2FA. Set '${role}TotpSecret' or '${role}RecoveryCode' in cypress.env.json. ` +
        `Use a dedicated disposable test account; do not enable 2FA on a shared account.`
      )
    }

    if (recoveryCode) {
      cy.contains('a', /Log in with a recovery code/i).click()
      cy.get('input[placeholder="RecoveryCode"]').type(recoveryCode, { log: false })
      cy.contains('button', /^Log in$/i).click()
      return
    }

    cy.task('totp', secret, { log: false }).then((code) => {
      cy.get('main input:visible').first().type(code, { log: false })
      cy.contains('button', /^Log in$/i).click()
    })
  })
}

const requiredEnv = (key, role) => {
  const value = Cypress.env(key)
  if (!value) throw new Error(`Missing Cypress environment value '${key}' required for role '${role}'.`)
  return value
}

Cypress.Commands.add('loginAs', (role) => {
  const account = roles[role]
  if (!account) throw new Error(`Unknown test role '${role}'. Expected one of: ${Object.keys(roles).join(', ')}.`)
  const email = requiredEnv(account.emailKey, role)
  const password = requiredEnv(account.passwordKey, role)

  cy.clearAllCookies({ log: false })
  cy.clearAllLocalStorage({ log: false })
  cy.clearAllSessionStorage({ log: false })

  cy.session([role, email, 'role-isolation-v3'], () => {
    cy.visit('/Account/Login')
    cy.get('input[placeholder="name@example.com"]').should('be.visible').type(email, { log: false })
    cy.get('input[placeholder="password"]').should('be.visible').type(password, { log: false })
    cy.contains('button', /^Log in$/i).should('be.enabled').click()
    completeTwoFactorChallenge(role)
    cy.location('pathname', { timeout: 20000 }).should('not.match', /\/Account\/Login(?:With2fa)?$/i)
  }, {
    cacheAcrossSpecs: true,
    validate() {
      cy.visit(account.landingPath)
      cy.location('pathname').should('not.match', /\/Account\/Login$/i)
    }
  })
})

Cypress.Commands.add('logout', () => {
  cy.contains('button, a', /^(Logout|Sign out)$/i).should('be.visible').click()
  cy.location('pathname', { timeout: 20000 }).should('match', /\/Account\/Login|\/$/i)
})

Cypress.Commands.add('api', (method, url, body, options = {}) => {
  return cy.request({ method, url, body, failOnStatusCode: options.failOnStatusCode ?? true })
})

Cypress.Commands.add('uniqueName', (prefix) => {
  return cy.wrap(`${prefix}-${Date.now()}-${Cypress._.random(1000, 9999)}`, { log: false })
})
