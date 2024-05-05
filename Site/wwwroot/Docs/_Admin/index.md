# _Admin

* Step 0: Admin --> Excel
    * SN
    * ICCID

* Step 1: Set up NB-IOT sensor node
    * Open sensor
    * Install SIM
        * Detach modem board
        * Insert SIM, reversed! (with corner to the outside)
        * Attach modem board
    * Install antenna
    * Connect UART
        * USB TX <=blauw=> NDDS RX (JP6 -- 2)
        * USB RX <=groen=> NDDS TX (JP6 -- 1)
        * (not used)
        * USB GND <=zwart/wit=> NDDS GND (JP6 -- 0)
        * Plug in USB TTL
    * Program sensor
        * Start minicom
            `minicom ndds75`
        * Connect battery jumper
        * Run commands:
            ```
            12345678

            # Echo
            ATE1

            # UDP
            AT+PRO=2

            # server3.foxinnovations.be
            AT+SERVADDR=128.199.50.226,55683

            # ?
            AT+CFM=1

            # Set 1nce
            AT+APN=iot.1nce.net

            # Extend network acquisition from 5 to 10 minutes
            AT+CSQTIME=10

            # Transmit every 2h
            AT+TDC=7200

            # Send 8 records
            AT+NOUD=8

            # Record every 2h
            AT+TR=7200

            # 2024-01-07: Lock 1nce SIM to Orange
            # TO BE TESTED FURTHER...
            AT+COPS=1,2,"20610"

            # Reboot
            ATZ
            ```
    * Close sensor
* Step 2: Onboarding Wateralarm
    * On server
        ``` bash
        Console account create --email <email>
        # --> store AccountID <ai>

        Console account setlink --ai <ai>
        Console account read --ai <ai>
        # --> store AccountLink
        
        Console sensor create --deveui <deveui>
        # --> store SensorID <si>

        Console sensor setlink --si <si>
        Console sensor read --si <si>
        # --> store SendorLink

        Console account addsensor --ai <ai> --si <si>
        Console account updatesensor --ai <ai> --si <si> --name Regenput --distanceempty 2500 --distancefull 500 --capacity 10000
        ```
* Troubleshooting 
    ```
    AT+COPS?
    AT+CEREG?
    AT+CFG
    AT+NUESTATS
    AT+CGPADDR
    --> +CGPADDR:1,10.138.62.162
    AT+NPING=128.199.50.226
    --> +NPING:128.199.50.226,51,4227
    ```
* 1nce
    * https://portal.1nce.com/ --> login
    * https://portal.1nce.com/portal/customer/sims --> SIM should be green

