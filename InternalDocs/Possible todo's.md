Possible todo's:

- Make the admin login flow in line with the regular login flow:
When going to the /adm page, and the user isn't logged, he still gets the Login button, but than the regular login page should be shown (with email and google login)
- Document " <span class="badge bg-warning">beta</span> "


Next is the removal of measurements.
- Enable the thrash can button (for users of the sensor, and admins).
- When clicked: show a confirmation message Do you want to remove the measurement at timestamp <date/time>? Yes/No/Cancel
- Add to the audit log.

I want an audit log of all actions done by the end user (later to be extended to admins as well).
This can be stored in a text file (ILogger? or plain text?)
Should contain: timestamp, logged in person, client IP-address, wateralarm account, wateralarm sensor (if applicable), action, details (if applicable).
Create a plan for this, both functional and technical.  No implementation yet.
