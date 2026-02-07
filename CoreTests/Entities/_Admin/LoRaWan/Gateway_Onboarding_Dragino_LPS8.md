
# LoRaWan Gateway 'Dragino LPS8'


## Update Wifi/networking

* (!) Temporarily allow incoming DHCP requests, or disable Linux firewall
* Temporarily change IP-address and run DHCP server on ethernet port:
  ```
  sudo ifconfig enp3s0 192.168.9.1 netmask 255.255.255.0
  sudo dnsmasq -d -C /dev/null --port=0 --domain=localdomain --interface=enp3s0 --dhcp-range=192.168.9.2,192.168.9.10,99h
  ```
* Connect LPS8 using ethernet cable to laptop, and power up
* This gives this output:
  ```
  dnsmasq: started, version 2.90 DNS disabled
  dnsmasq: compile time options: IPv6 GNU-getopt DBus no-UBus i18n IDN2 DHCP DHCPv6 no-Lua TFTP conntrack ipset no-nftset auth cryptohash DNSSEC loop-detect inotify dumpfile
  dnsmasq-dhcp: DHCP, IP range 192.168.9.2 -- 192.168.9.10, lease time 4d3h
  dnsmasq-dhcp: DHCPDISCOVER(enp3s0) a8:40:41:...
  dnsmasq-dhcp: DHCPOFFER(enp3s0) 192.168.9.9 a8:40:41:... 
  dnsmasq-dhcp: DHCPDISCOVER(enp3s0) a8:40:41:... 
  dnsmasq-dhcp: DHCPOFFER(enp3s0) 192.168.9.9 a8:40:41:... 
  dnsmasq-dhcp: DHCPREQUEST(enp3s0) 192.168.9.9 a8:40:41:1d:15:e6 
  dnsmasq-dhcp: DHCPACK(enp3s0) 192.168.9.9 a8:40:41:... dragino-1d15e4
  ```
* So IP address is `192.168.9.9`
* Surf to http://192.168.9.9 in browser
* Replace Wifi network configuration by scanning for new networks. 