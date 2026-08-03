# Backlot License FAQ

Our **core library** is available under **two licenses**:
- **AGPLv3** (open-source).
- **Commercial License** (proprietary).

**End users** who distribute our core (e.g., in their applications) **must accept either AGPLv3 or the commercial license**.

---

## Can I create a nuget package that references Backlot libraries?

**Yes!** We welcome contributions from the community. You can write plugins, libraries, and tools that **reference our core via NuGet** and license your own code under; MIT, Apache 2.0 or AGPL 3.0. When using AGPL 3.0 we advice you to use a dual license, like us. When you would like to use another license for your work, feel free to contact us.

## Best Practices for plugin/library authors
### Do:

- Reference our core via NuGet.
- License your code under MIT or Apache 2.0 to allow permissive use of your contribution.
- Document that end users must accept our **AGPL 3.0 or commercial terms** when distributing the final application.

### Do Not:

- **Redistribute our core** (e.g., bundling our DLLs or our source code in your repo).
- **Modify our core** without accepting our CLA. We only accept contributions to our core via our contribution policies.


---

## FAQ for authors
### Q: Can I publish my plugin under MIT if it references your AGPL core via NuGet?

**A:** **Yes.** Your plugin is a **separate work** as long as it does not modify or redistribute our core. The **distributor** of the final application (not you) is responsible for accepting AGPL 3.0 or commercial terms when they distribute the final application.
### Q: Can I use your core in a  SaaS product?

**A:** **Yes, but.** you must accept **AGPL or the commercial license**. Your SaaS product eventually **redistributes our core**, AGPL 3.0 terms apply. For proprietary use, a commercial license is required.
### Q: What license should I use for my standalone plugin or library?

**A:** For **independent plugins, libraries, SDKs, connectors, and example code**, **MIT or Apache 2.0** are appropriate. These licenses align with our goal of keeping components **community-friendly**.
### Q: What if I want to create a closed-source or proprietary product with Backlot?

**A:** In this case your product will eventually **redistribute our core**, a **commercial license** is necessary.

---

> _This FAQ is intended as practical guidance only and does not replace the terms of AGPLv3 or any commercial license agreement. If there is any inconsistency, the applicable license terms prevail._ _For specific use cases, please contact us._
