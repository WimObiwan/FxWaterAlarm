
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

## 2. Register in TTN

* Go to TTN --> Application
	* https://eu1.cloud.thethings.network/console/applications/fx-dragino-ldds75 
	* "Select the end device in the LoRaWAN Device Repository"
	* "Dragino"
	* "LDDS75"
	* "Unknown"
	* "1.2" (=last)
	* "EU 863 870"
	* "Europe 863-870 MHz (SF9 for RX2)"
	* JoinEUI (=AppEui)
	* Id: fx-waterlevel2-<number>
* Set name fx-ttn-...
* Set location
	* https://whatismyelevation.com/
* Set payload formatter!

## 3. Turn on device

* DIP switch

## 4. Set update frequency using downlink packet

* Interval 5 minutes  
  `0100012C`
* Interval 20 minutes:  
  `010004B0`
* Interval 2 uur:  
  `01001C20`
