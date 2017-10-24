Ef.Audit (obsolete, the new version is [EF.Audit.Core](https://github.com/biasmey/EF.Audit.Core))
========
Easy audit capabilities for EF projects.

Auditable Entities
===================
First of all, you need to mark all your auditable entities using _"Auditable"_ and _"NotAuditable_" attributes.

**Auditable:** Can be used to mark a class (including all properties), or a property as auditable.
**NotAuditable:** Marks a property as not auditable.

IMPORTANT: All your auditable entities needs to be serializable.

Enable Audit
=============
1. Install EF.Audit nuget package.
2. Implement _"IAuditDbContext"_ interface in your context.
3. Whenever you need to save context changes call _"SaveChangesAndAudit"_ method instead of _"SaveChanges"_ one.

Authors and Contributors
========================
Biasmey Morgado Guirola (biasmey), 
José Antonio Plá Rodríguez (jpla2005)

Contributions
=============
Any contribution is welcomed.
