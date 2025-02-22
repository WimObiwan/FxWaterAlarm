---
title: Versie Informatie
---

# Versie Informatie

<!--
### vNext

> yyyy-mm-dd
>
> ##### Nieuw
> *
> *
>
> ##### Verbeteringen
> *
> *
>
> ##### Andere
> *
> *
-->

### v1.0
> 2025-02-22
> * Ondersteuning voor nieuwe sensoren voor "Detectie" LWL02, LWL03.  Inclusief alarmen, maar voorlopig zonder grafiek.
> * Grafieken werden vereenvoudigd: geen 'Grafiek', maar meteen Volume, Percentage of Hoogte. 
> * Boven de grafieken worden nu links getoond om snel in te zoomen naar laatste week, laatste 3 weken, laatste 3 maanden en laatste jaar.

### v0.36
> 2024-07-17
> ##### Nieuw
> * Ondersteuning voor alarmen op hoogte (voorlopig nog niet in te stellen door eindgebruiker)
> * (Admin Powershell interface voor alarmen)

### v0.35
> 2024-07-17
> ##### Andere
> * Vanaf nu wordt *Capaciteit* gebruikt voor de maximale inhoud van de put, en *Volume* voor de huidige inhoud van de put op een bepaald moment. 
> ##### Verwijderingen
> * (Console toepassing werd verwijderd, enkel Powershell interface is ondersteund vanaf nu)

### v0.34
> 2024-07-15
> ##### Nieuw
> * Extra grafieken voor batterij, signaal, hoogte, afstand en percentage

### v0.33
> 2024-07-14
> ##### Nieuw
> * Optioneel afzetten van de min/max afkapping, nuttig voor putten die veel boven 100% volume hebben
> * Experimenteel gebruik van nieuwe visuals, nieuwe grafieken

### v0.32
> 2024-06-18
> ##### Andere
> * Demo changes
> * (Admin Powershell interface voor intern gebruik)

### v0.31
> 2024-05-12
> ##### Nieuw
> * Melding-mails worden slechts éénmaal gestuurd, en niet langer elke nacht herhaald

### v0.30
> 2024-05-12
> ##### Nieuw
> * Melding-mails met variabele limiet (voorlopig nog niet door de gebruiker aan te passen)

### v0.29
> 2024-05-10
> ##### Nieuw
> * Melding-mails

### v0.28
> 2024-05-05
> ##### Nieuw
> * Sensor informatie voor interne acties

### v0.27
> 2024-04-23
> ##### Nieuw
> * Documentatie-pagina's
> * Link instellen in de mobiele app
> ##### Verbeteringen
> * Grafiek: Verwijderd 6h
> * Grafiek: Hou rekening met tijdzone/zomertijd/wintertijd-verschuiving

### v0.26
> 2024-03-14
> ##### Nieuw
> * API: Trends, HeightMm & DistanceMm
> ##### Andere
> * .net 8 upgrade, upgrade dependencies

### v0.25
> 2024-03-10
> ##### Nieuw
> * Ondersteuning voor metingen zonder volume, in dit geval wordt overal "%"
>   getoond i.p.v. "L"
> * Ondersteuning voor metingen zonder astand vol, in dit geval wordt overal "mm"
>   getoond i.p.v. "L" of "%"
> * API: AccountSensor endpoint
> * Console: Account ListSensor, RemoveSensor

### v0.24
> 2023-12-27
> ##### Verbeteringen
> * Account pagina: Automatische refresh
> * Demo pagina: Login gedesactiveerd

### v0.23
> 2023-12-06
> ##### Nieuw
> * AccountSensor pagina: Gebruikers kunnen de capaciteit van hun eigen put beheren.

### v0.22
> 2023-12-02
> ##### Nieuw
> * Passwordless login

### v0.21
> 2023-11-20
> ##### Nieuw
> * WaterAlarm kan nu geïnstalleerd worden als app (PWA) op ondersteunde toestellen. 
