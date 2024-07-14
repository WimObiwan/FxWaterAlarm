# Provisioning

``` powershell
$email = 'email@domain.tld'
$devEui = 'xxxx'
$accountSensorDetails = @{
    Name = 'Regenput-LoRa',
    DistanceEmptyMm = 2700,
    DistanceFullMm = 700,
    CapacityL = 10000,
    AlertsEnabled = $true
}

$account = Get-WAAcount -Email $email
if (-not $account)
{
    $account = New-WAAccount -Email $email
    Reset-WAAccountLink $account
    $account = Get-WAAccount $account.Id
}
Write-Warning "Using Account: Id=$($account.Id), Link=$($account.Link)"

$sensor = Get-WASensor -DevEui $devEui
if (-not $sensor)
{
    $sensor = New-WASensor -DevEui $devEui
    Reset-WASensorLink $sensor
    $sensorw = Get-WASensor $sensor.Id
}
Write-Warning "Using Sensor: Id=$($sensor.Id), Link=$($sensor.Link)"

$accountSensor = Get-WAAccountSensor -Account $account | ?{ $_.SensorId -eq $sensor.Id }
if (-not $accountSensor)
{
    Add-WAAccountSensor -Account $account -Sensor $sensor
}

Add-WAAccountSensorDefaultAlarms -Account $account -Sensor $sensor
Set-WAAccountSensor -Account $account -Sensor $sensor @accountSensorDetails
```