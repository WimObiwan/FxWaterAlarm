# Measurement actions

``` bash
# Count measurements
influx -username $user -password $password -database wateralarm \
  -execute "select count(*) from waterlevel where DevEUI = '<DEVEUI>' group by * order by desc"

# Remove measurements
influx -username $user -password $password -database wateralarm \
  -execute "delete from waterlevel where time <= '2024-06-29T00:00:00Z' and DevEUI = '<DEVEUI>'"

# Copy measurements to another sensor:
influx -username $user -password $password -database wateralarm \
    -execute "select * from waterlevel where time <= '2024-03-04T16:00:00Z' and DevEUI = '<DEVEUI>' group by * order by desc" -format csv \
    > export.csv
cat ./export.csv | %{
  $items = $_.Split(',')
  "waterlevel,DevEUI=<DEVEUI> RSSI=$($items[3]),batV=$($items[4]),distance=$($items[5]),distance_raw=$($items[6]) $($items[2])"
} | Out-File import.txt
./import.txt | ?{ $_ -notmatch ',distance=,' } | Out-File import.txt
# !!! Prefix with header line (# DML...)
influx -username $user -password $password -database wateralarm -import -path=import.txt
```

## Import file example
```
# DML
# CONTEXT-DATABASE: wateralarm

waterlevel,DevEUI=A801234567890123 RSSI=-111,batV=3.296,distance=691,distance_raw=691 1718625040899336400
waterlevel,DevEUI=A801234567890123 RSSI=-111,batV=3.285,distance=581,distance_raw=581 1718609954000000000
waterlevel,DevEUI=A801234567890123 RSSI=-111,batV=3.3,distance=371,distance_raw=371 1718579204000000000
waterlevel,DevEUI=A801234567890123 RSSI=-111,batV=3.265,distance=308,distance_raw=308 1718541989877111300
waterlevel,DevEUI=A801234567890123 RSSI=-111,batV=3.311,distance=296,distance_raw=296 1718464292000000000
waterlevel,DevEUI=A801234567890123 RSSI=-111,batV=3.293,distance=333,distance_raw=333 1718457177346782100
waterlevel,DevEUI=A801234567890123 RSSI=-111,batV=3.302,distance=301,distance_raw=301 1718410793003455400
waterlevel,DevEUI=A801234567890123 RSSI=-111,batV=3.313,distance=610,distance_raw=610 1718364988000000000
waterlevel,DevEUI=A801234567890123 RSSI=-111,batV=3.274,distance=679,distance_raw=679 1718342164072528100
waterlevel,DevEUI=A801234567890123 RSSI=-111,batV=3.317,distance=685,distance_raw=685 1718327111000000000
```