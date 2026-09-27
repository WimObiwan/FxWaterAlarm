---
title: Prijzen
date: 2026-09-27
---

# Prijzen

Alle prijzen zijn in euro en **exclusief btw**.  Het zijn de huidige prijzen; ze kunnen wijzigen.
Welke sensor je nodig hebt, staat uitgelegd op [Sensor Overzicht](Sensor_Overzicht.md).

## Sensoren

Eenmalige aankoopprijs per sensor.  Elke sensor bestaat in een LoRa- en een 5G-uitvoering.

| Sensor | Wat meet hij? | **LoRa** | **5G (NB-IoT)** |
|--------|---------------|---|---|
| Ultrasoon | Waterniveau, gemeten van bovenaf | **140 €** | 160 € |
| Druk | Water- of stookolieniveau, drukcel in de vloeistof | 220 € | 240 € |
| Bodemvochtigheid | Vochtigheid, temperatuur en geleidbaarheid van de bodem | 180 € | 200 € |
| Lekdetectie | Water aanwezig of niet, via detectiekabel | 140 € | 160 € |

* Een **LoRa-sensor is telkens 20 € goedkoper** dan dezelfde sensor in 5G-uitvoering, maar vereist
  wel een gateway (zie hieronder).
* De **ultrasone niveausensor is het voordeligst**; een druksensor kost 80 € meer, maar is
  ongevoelig voor buizen, kabels en smalle schachten.

## Opties bij een druksensor

De druksensor bestaat uit een drukcel in het water, een kabel en de sensormodule met de antenne.
Wil je de module (of enkel de antenne) verder van de put plaatsen — bijvoorbeeld bovengronds, voor
een betere 5G-ontvangst — dan kan dat met een langere kabel.  De meerprijs van de leverancier
wordt gewoon doorgerekend.

| Optie | Meerprijs |
|---|---|
| Kabel drukcel 10 m in plaats van 5 m | + 20 € |
| Kabel drukcel 15 m in plaats van 5 m | + 30 € (niet leverbaar met zonnepaneel) |
| Verlengkabel voor de antenne, 10 m | + 15 € |

* De standaardlengte van de kabel naar de drukcel is **5 meter**; die kabel kan je **niet
  inkorten** (oprollen mag).
* Welke opstelling in jouw put zinvol is, staat op
  [Druksensor Opstelling](Sensor_Nodes/Druksensor_Opstelling.md).

## Gateway (enkel bij LoRa)

| Onderdeel | Prijs |
|---|---|
| LoRa-gateway | **130 €** |

* Eenmalige aankoop.  Enkel nodig bij LoRa-sensoren; bij 5G niet.
* **Eén gateway bedient meerdere sensoren** op dezelfde locatie.
* De gateway staat binnen, heeft **stroom** nodig en een **WiFi- of ethernetverbinding**.

## Abonnement

| Formule | Prijs per sensor |
|---|---|
| Jaarlijks | **30 € / jaar** |
| Maandelijks | 5 € / maand |

* **Het eerste jaar is inbegrepen** in de aankoopprijs van de sensor.  Het abonnement start pas
  daarna, dus na 12 maanden.
* De prijs geldt **per sensor**, niet per account of per gateway.

Het abonnement dekt het gebruik van het WaterAlarm-platform: het dashboard met de actuele waarden,
de historiek en grafieken, de alarmen per e-mail, en de integraties
([API](Integraties/API.md), [Home Assistant](Integraties/Home_Assistant.md)).

**Ook inbegrepen, zonder meerprijs:**

* **De simkaart van een 5G-sensor.**  De dataverbinding zit mee in het abonnement — je hoeft zelf
  geen simkaart te kopen en geen contract bij een mobiele operator af te sluiten.  Er zijn dus
  geen bijkomende kosten voor de verbinding, ook niet bij 5G.
* **De batterij.**  Is de batterij van je sensor leeg, dan krijg je een nieuwe, tot één batterij
  per sensor per jaar.  Enkel de eventuele **verzendingskosten** komen daar nog bij.
* **Het bevestigingshoudertje.**  Bij een niveausensor zit het bijhorende houdertje mee: dat voor
  de kabel bij een druksensor, dat voor de kegel bij een ultrasone sensor — zie de
  [Sensor Nodes](Sensor_Nodes/) voor de montage.
* **Het zonnepaneel.**  De meeste sensoren zijn leverbaar met een ingebouwd zonnepaneel; bij een
  installatie met abonnement rekenen we daarvoor geen meerprijs aan.  Het paneel zit **vast op de
  sensormodule**, dus het is vooral interessant waar die module zelf voldoende daglicht krijgt —
  zie [Sensor Overzicht](Sensor_Overzicht.md#zonnepaneel).

## Installatie

Je installeert de sensor normaal gezien zelf.  Dat is eenvoudig: twee schroeven voor de
sensormodule en twee voor het bijgeleverde houdertje — zie de [Sensor Nodes](Sensor_Nodes/) voor
de montage.

Liever een installatie ter plaatse?  Dat kan, aan een vaste prijs volgens de afstand tot Torhout:

| Afstand | Prijs |
|---|---|
| Minder dan 30 km | **50 €** |
| Minder dan 60 km | **75 €** |
| Minder dan 100 km | **100 €** |
| Verder | Op aanvraag |

* Eenmalig, per verplaatsing: meerdere sensoren op dezelfde locatie kosten niet meer.
* Bij een installatie ter plaatse vervalt de verzending.

## Verzending

Verzending gebeurt met bpost.  Het bedrag dekt de verzending zelf én de verpakking:

| Bestemming | Prijs |
|---|---|
| Naar een postpunt of pakjesautomaat | **7,44 €** |
| Naar een adres | 9,92 € |

* Dit zijn de tarieven op het moment van schrijven; ze kunnen wijzigen.  Op de bestelbon staat
  het bedrag dat effectief geldt.
* Bij een **installatie ter plaatse** vervalt de verzending (zie hierboven).
* De verzending van een **vervangbatterij** (inbegrepen in het abonnement, zie hierboven) komt
  apart.

## Rekenvoorbeelden

Alle bedragen exclusief btw.

### Eén regenput, geen internet in de buurt

| Onderdeel | Prijs |
|---|---|
| Ultrasone sensor, 5G | 160 € |
| **Eenmalig** | **160 €** |
| Abonnement vanaf het tweede jaar | 30 € / jaar |

### Eén regenput, WiFi in de buurt

| Onderdeel | Prijs |
|---|---|
| Ultrasone sensor, LoRa | 140 € |
| LoRa-gateway | 130 € |
| **Eenmalig** | **270 €** |
| Abonnement vanaf het tweede jaar | 30 € / jaar |

### Drie meetpunten op dezelfde locatie, met WiFi

Twee regenputten en een lekdetectie in de kelder:

| Onderdeel | Prijs |
|---|---|
| 2 × ultrasone sensor, LoRa | 280 € |
| 1 × lekdetectie, LoRa | 140 € |
| LoRa-gateway (één voor alle sensoren) | 130 € |
| **Eenmalig** | **550 €** |
| Abonnement vanaf het tweede jaar (3 sensoren) | 90 € / jaar |

### Dezelfde drie meetpunten, in 5G

| Onderdeel | Prijs |
|---|---|
| 2 × ultrasone sensor, 5G | 320 € |
| 1 × lekdetectie, 5G | 160 € |
| Gateway | — |
| **Eenmalig** | **480 €** |
| Abonnement vanaf het tweede jaar (3 sensoren) | 90 € / jaar |

> **Waarom is 5G hier goedkoper?**  De gateway kost 130 €, terwijl je per sensor slechts 20 €
> bespaart met LoRa.  Kies LoRa dus in de eerste plaats omwille van de betrouwbaarheid — zeker bij
> diepe betonnen putten of metalen deksels, waar 5G-ontvangst een probleem kan zijn.

## Wat als een sensor toch niet past?

Niet elke sensor is geschikt voor elke put of omgeving.  Beschrijf je situatie daarom vooraf, dan
kiezen we samen het juiste toestel.  Loopt het toch mis — bijvoorbeeld een ultrasone sensor die
valse echo's meet, of een 5G-sensor met te weinig ontvangst — dan is de oplossing meestal een
upgrade naar een druksensor of een overstap naar LoRa.  Wat er kan mislopen en hoe je het oplost,
staat bij [Wat kan er misgaan?](Sensor_Overzicht.md#wat-kan-er-misgaan).

### Omruilen

**Bij een omruiling betaal je enkel het prijsverschil.**  Blijkt de 5G-ontvangst ter plaatse
onvoldoende, of meet een ultrasone sensor niet betrouwbaar in jouw put, dan ruilen we om naar de
oplossing die wél werkt — je betaalt het verschil in aankoopprijs, plus de verzending.  Je blijft
dus niet met een onbruikbare sensor zitten.

Dat is in de praktijk bijna altijd de betere afloop: je houdt een werkende installatie.

### Terugname

Lukt de installatie om een andere reden niet, dan neem ik alles terug **tot zes maanden na
levering**, zolang het toestel onbeschadigd is:

* Je krijgt de **aankoopprijs volledig terugbetaald**.
* De **verzendingskosten** (heen en terug) blijven ten laste van de koper.
* Vanaf **één maand** na levering wordt **5 € per maand** afgehouden voor het gebruik van batterij
  en simkaart.

Een terugname is tot nu toe nog nooit nodig geweest; een omruiling wel.
