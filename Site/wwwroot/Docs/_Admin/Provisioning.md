# Provisioning

``` powershell
$email = 'email@domain.tld'

$account = New-WaAccount -Email $email
$account.AccountId
$account.Link

Set-WaAccount -AccountId $accoutId -Email $email -Link $link

Get-WaAccount -Email $email
Get-WaAccount -AccountId $accountId

$ai = . Console --email $email --format json
