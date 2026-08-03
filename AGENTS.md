# Agent Context: StockSense (.NET Blazor)

This repository uses **BlazorBlueprint** (a shadcn/ui-inspired component library for .NET Blazor using pre-built Tailwind CSS). Always adhere to the framework architectural rules and critical execution rules below.

---

## 📚 Core Library Reference

- **Complete LLM Documentation:** If you need specific component API specs, primitives, syntax, or installation schemas, fetch or read from the [BlazorBlueprint Consolidated LLM File](https://blazorblueprintui.com).
- **Startup & Registration:** Reference the [BlazorBlueprint Setup Guide](https://blazorblueprintui.com/llms/setup.txt) to verify service lifetimes like `builder.Services.AddBlazorBlueprintComponents()`.

---

## 🛠️ Code Implementation Rules

- **Do Not Setup Tailwind:** The Tailwind CSS configuration is pre-built into the library. Do not install npm Tailwind packages unless explicitly requested.
- **Overlay Primitives:** Every layout file containing overlay systems (dialogs, drawers, popovers, context menus) must wrap or include the `<BbPortalHost />` component. Do not attempt to manually control z-indexing outside of it.
- **Dark Mode:** Utilize the `.dark` class hierarchy natively supported by the components.
- **Component Architecture:** Separate custom styling using the two-tier layout system:
    - Use **Styled Components** for rapid, default layouts.
    - Use **Headless Primitives** (`<BbPrimitive...`) only when complete structural overrides or unique design system elements are demanded.
- **Package Management:** Always use `dotnet` CLI variants to add UI pieces or manage package dependencies rather than Node.js packages.
- **Verification:** Before concluding a component refactor task, check for keyboard navigation tags and appropriate ARIA attributes mapping back to the primitive specifications.

---

## 📱 Mobile & UI Conversion Rules

- **Full playbook:** `docs/mobile-conversion.md` (auto-loaded via `opencode.json` instructions).
- **No inline styles:** All `style="..."` attributes must use Tailwind utilities. Dynamic C# values are the only exception.
- **Tables → cards on mobile:** Every `<table>` needs a `md:hidden` card stack fallback. Horizontal scroll at `< 768px` is banned.
- **Delete companion `.razor.css` files** once all rules are migrated to Tailwind.

---

## 🤖 Critical Execution Rules

### RESPONSES
- Keep responses concise and to the point — unless the user asks otherwise.

### PLANNING MODE
- Always ask clarifying questions.
- Never assume design, tech stack, or features.
- Use deep-dive sub-agents to assist with research.
- Use deep-dive sub-agents to review different aspects of your plan before presenting to the user.

### CHANGE / EDIT MODE
- Never implement features yourself when possible — use sub-agents!
- Identify changes from the plan that can be implemented in parallel, and use sub-agents to implement features efficiently.
- When using sub-agents to implement features, act as a coordinator only.
- Use the best model for the task — premium models for complex tasks (like coding) and mid-tier models for simpler tasks like documentation.
- After completing features (large or small), always run `dotnet build` and check for warnings or errors before concluding.

### DATABASE SCHEMA CHANGES
- Whenever you make changes to the database schema, ALWAYS run `dotnet ef migrations add <MigrationName>` followed by `dotnet ef database update`.
- NEVER manually edit existing migration files.
- NEVER use `dotnet ef database drop` without explicit user confirmation.

### TESTING
- Use any testing tools, libraries, or MCP tools available to the project for testing your changes.
- Never assume your changes simply work — always test!
- If the project does not have any testing tools, scripts, or MCP tools available for testing, ask the user whether testing should be skipped.

---

