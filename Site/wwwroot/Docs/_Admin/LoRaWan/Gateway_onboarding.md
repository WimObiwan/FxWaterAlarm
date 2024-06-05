
# LoRaWan Gateway

Onboarding procedure

# 1. Information

* Take photograph of backside gateway  
  Android: Gallery --> "Copy all"
  For:
	* WIFI MAC: e.g. 58A0CB800123
	* WIFI Password
	* Gateway EUI is derived from WIFI MAC, by putting FFFE in between:
		* 58A0CB__FFFE__800123

# 2. Register in TTN ("Claim")

* Go to TTN --> Gateways
	* https://eu1.cloud.thethings.network/console/gateways
	* Enter Gateway EUI (with "FFFE")
	* Claim authentication code: = Wifi password
	* ID: leave eui-... (?)
	* Frequency plan: Europe 863-870MHz (recommended)
* Set name fx-ttn-...
* Set location

# 3. Set Wifi password (initially)

* Plug in (with "reset" at the bottom)
* Press reset for 5 seconds (LED blinks red/green)
* Press setup for 10 seconds (LED blinks red fast)
* Connect to WIFI SSID e.g. "MINIHUB-800123" (xxxxxx is last 6 of EUI)
* Connect to http://192.168.4.1  
* Enter WIFI password
* Add WIFI SSID e.g. "Obiwan", with SSID password
* Click Save & Reboot 

# 4. Change Wifi password

* Press setup for 10 seconds
* Connect to WIFI SSID e.g. "MINIHUB-800123" (xxxxxx is last 6 of EUI)
* Connect to http://192.168.4.1  
* ...
* Update location in TTN
	* https://eu1.cloud.thethings.network/console/gateways

# Other info
 
* Required firewall configuration: (OUT)
	* TCP 443   CUPS
	* TCP 8887  LNS
	* TCP 9191  ROOT CUPS
	* UDP 53  DNS