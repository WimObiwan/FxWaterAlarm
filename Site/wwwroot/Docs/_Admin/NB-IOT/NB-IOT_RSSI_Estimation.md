# NB-IOT RSSI Estimation

## RSSI Calculations

* RSSI -3 ==> signal x2 
  * RSSI -15 ==> 2^5 = x32 
* -111 dBm = minimum (for NB-IOT) (For LoRaWan, possibly -130 dBm)

| Location | Setup | Operator | RSSI | Remarks |
|-|-|-|-|-|
| Home | Kast | Orange | -109 dBm |
| F.T. | Put dicht | Orange| -111 dBm... |
| F.T. | Put open | Orange| -111 dBm | (reception <8%) |
| F.T. | Put dicht | Proximus | -111 dBm | (reception ~40%) |
| F.T. | Put open | Proximus | -97 dBm | +15 => 2^5 = x32 |
| F.T. | Binnen | Proximus | -93 dBm | 
| M.V.H. | Put | Orange | -82 dBm |
