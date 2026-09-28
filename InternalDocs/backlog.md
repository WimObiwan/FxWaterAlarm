Possible todo's:

- Make the admin login flow in line with the regular login flow:
When going to the /adm page, and the user isn't logged, he still gets the Login button, but than the regular login page should be shown (with email and google login)
- Logout open redirect: `AccountCallback` redirects to the `url` parameter without an `IsLocalUrl` check (see state/2026-09-28-keycloak-logout.md).
- Document " <span class="badge bg-warning">beta</span> "

For my customers using LoRaWan, I would like them to set the frequency of measurements to separate values, for example:
- 10 minutes
- 30 minutes
- 1 hour
- 2 hours
- 6 hours
- 12 hours
- 24 hours
This can be done technically by scheduling a downlink message to the device via TheThingsNetwork.
E.g. 