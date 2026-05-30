
# LoRaWan Gateway

Onboarding procedure

## 1. Information

* Take photograph of backside gateway  
  Android: Gallery --> "Copy all"
  For:
	* SN
	* DevEui
	* AppEui
	* AppKey

## 2. Manual device types

| Device | Frequency plan | Spec | Reg. | End Dev ID |
|--------|----------------|------|------|------------|
| DDS75-LB | EU 863-870 MHz (SF9 for RX2 - recommended) | 1.0.3 | RP001-1.0.3-revA | fx-waterlevel3-6 |

JoinEUI = AppEui

## 3. Set update frequency using downlink packet

Both LDDS75 and DDS75-LB

Admin command examples are documented in `TTN_Downlink_Admin.md`.

* Interval 5 minutes  
  `0100012C`
* Interval 20 minutes:  
  `010004B0`
* Interval 2 uur:  
  `01001C20`
* Interval 12 uur:
  `0100A8C0`
* Interval 24 uur:  
  `01015180`
