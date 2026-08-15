---
title: Prijzen
date: 2026-08-14
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
| Maandelijks | 4 € / maand |

* **Het eerste jaar is inbegrepen** in de aankoopprijs van de sensor.  Het abonnement start pas
  daarna.
* De prijs geldt **per sensor**, niet per account of per gateway.
* Jaarlijks betalen is voordeliger: 12 × 4 € = 48 € tegenover 30 € per jaar.

Het abonnement dekt het gebruik van het WaterAlarm-platform: het dashboard met de actuele waarden,
de historiek en grafieken, de alarmen per e-mail, en de integraties
([API](Integraties/API.md), [Home Assistant](Integraties/Home_Assistant.md)).

**Ook inbegrepen, zonder meerprijs:**

* **De simkaart van een 5G-sensor.**  De dataverbinding zit mee in het abonnement — je hoeft zelf
  geen simkaart te kopen en geen contract bij een mobiele operator af te sluiten.  Er zijn dus
  geen bijkomende kosten voor de verbinding, ook niet bij 5G.
* **Het zonnepaneel.**  De meeste sensoren zijn leverbaar met een ingebouwd zonnepaneel; bij een
  installatie met abonnement rekenen we daarvoor geen meerprijs aan.  Het is vooral interessant
  waar de sensor voldoende daglicht krijgt — zie
  [Sensor Overzicht](Sensor_Overzicht.md#zonnepaneel).

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
