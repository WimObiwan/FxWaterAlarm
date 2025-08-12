---
title: API
---

# API

## Data ophalen

Je kunt je gegevens op halen in Json formaat.  Voeg hiervoor `api/` tussen in de 
persoonlijke link voor je sensor.

Bvb:
* Sensor link  
  https://www.wateralarm.be/a/demo/s/i5WOmUdoO0
* API link  
  https://www.wateralarm.be/api/a/demo/s/i5WOmUdoO0

## Alarm controle activeren

Je kunt de alarm controle handmatig activeren via een POST request naar:

```
POST /api/s/{SensorLink}/check-alarms
```

Parameters:
* `SensorLink`: De link identifier van de sensor
* `accountLink` (optioneel): De account link parameter als query string

Bvb:
```bash
curl -X POST https://www.wateralarm.be/api/s/i5WOmUdoO0/check-alarms
```

Deze endpoint voert dezelfde functionaliteit uit als de PowerShell cmdlet `Invoke-WACheckAccountSensorAlarms` en zal e-mail alarmen versturen indien nodig.
