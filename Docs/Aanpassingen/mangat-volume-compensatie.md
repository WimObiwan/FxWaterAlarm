---
title: Mangat Volume Compensatie
date: 2026-02-01
---

# Mangat Volume Compensatie

## Wat is het probleem?

Veel regenputten of wateropslagtanks hebben bovenaan een **mangat** (toegangsschacht) dat smaller
is dan de put zelf.  Wanneer het waterniveau stijgt tot boven de 100%-markering, stroomt het water
in dit smallere mangat.

Zonder compensatie weet WaterAlarm niet dat het mangat smaller is.  Het systeem gaat ervan uit dat
de hele put even breed is, en overschat daardoor het volume water bij overflow.  Of het kapt het
niveau gewoon af op 100%, waardoor je niet ziet hoeveel water er werkelijk nog bijkomt.

Met de **mangat volume compensatie** wordt dit opgelost: je geeft de oppervlakte van het mangat op,
en WaterAlarm berekent het correcte volume — ook boven de 100%.

## Hoe werkt het?

### Situatie: waterstand onder 100%

Wanneer het water onder het "vol"-niveau zit, verandert er niets.  Het volume wordt berekend op
basis van de inhoud en afmetingen van de put, zoals altijd.

```
          ┌──────┐
          │mangat│  ← smal (bv. 0,5 m²)
          │      │
     ┌────┘      └─────┐
     │    hoofdput     │  ← breed (bv. 4,5 m²)
     │                 │
     │~~~~~~~~~~~~~~~~~│  ← waterniveau (bv. 80%)
     │/////////////////│
     │/////////////////│
     │/////////////////│
     └─────────────────┘
```

> In dit geval is de berekening gewoon:
> **Volume = niveau × inhoud van de put**

### Situatie: waterstand boven 100% (overflow in het mangat)

Wanneer het water boven de 100%-markering stijgt, komt het in het mangat terecht.  Omdat het mangat
smaller is, komt er **minder volume per mm stijging** bij.

```
          ┌──────┐
          │//////│  ← water in het mangat
          │//////│
     ┌────┘══════└─────┐  ← 100% niveau (overgang put → mangat)
     │/////////////////│
     │/////////////////│
     │/////////////////│
     │/////////////////│
     │/////////////////│
     └─────────────────┘
```

> De berekening wordt nu:
> **Totaal volume = inhoud van de put + (overflow hoogte × mangat oppervlakte)**

### Vergelijking: met en zonder compensatie

Stel je hebt een put van **10.000 liter** met een bruikbare hoogte van **2.000 mm**, en een mangat
van **1,0 m²**.  De sensor meet een waterniveau van 125% (= 500 mm boven vol).

|                          | Zonder compensatie | Met compensatie |
|--------------------------|--------------------|-----------------|
| Hoofdput volume          | 10.000 L           | 10.000 L        |
| Overflow hoogte          | 500 mm             | 500 mm          |
| Extra volume in mangat   | *(niet berekend)*  | 500 L           |
| **Totaal weergegeven**   | 10.000 L (100%)    | **10.500 L (≈105%)** |

Zonder compensatie zie je gewoon "100%".  Met compensatie zie je het werkelijke volume, inclusief
het water dat in het mangat staat.

## Hoe stel je het in?

### Stap 1: Bepaal de oppervlakte van je mangat

Meet de binnenmaat van je mangat (de opening bovenaan de put).

- **Rond mangat:** oppervlakte = π × (straal in meter)²
  - Voorbeeld: diameter 80 cm → straal = 0,4 m → oppervlakte = 3,14 × 0,16 = **0,50 m²**
- **Vierkant mangat:** oppervlakte = zijde × zijde (in meter)
  - Voorbeeld: 70 cm × 70 cm → 0,7 × 0,7 = **0,49 m²**

```
     Rond mangat              Vierkant mangat
    ┌───────────┐            ┌───────────┐
    │  ╭─────╮  │            │ ┌───────┐ │
    │  │ ⌀80 │  │            │ │ 70×70 │ │
    │  │ cm  │  │            │ │  cm   │ │
    │  ╰─────╯  │            │ └───────┘ │
    │   0,50 m² │            │   0,49 m² │
    └───────────┘            └───────────┘
```

### Stap 2: Instelling invoeren in WaterAlarm

1. Ga naar de **sensorpagina** van je put op WaterAlarm.
2. Klik op **Instellingen** (het tandwieltje).
3. Zoek het veld **Mangat oppervlakte**.
4. Vul de oppervlakte in **m²** in (gebruik een punt of komma als decimaalteken).
5. Klik op **Opslaan**.

```
     ┌──────────────────────────────────────────────┐
     │          Sensor Instellingen                 │
     │                                              │
     │  Afstand leeg (mm):     [ 3000       ]       │
     │  Afstand vol (mm):      [ 800        ]       │
     │  Inhoud (L):            [ 10000      ]       │
     │  Mangat oppervlakte:    [ 0,50       ] m²    │
     │                                              │
     │              [ Opslaan ]                     │
     └──────────────────────────────────────────────┘
```

> **Tip:** Als je de oppervlakte van je mangat niet kent of je put geen mangat heeft, laat het veld
> dan gewoon leeg.  WaterAlarm werkt dan zoals voorheen (niveau wordt afgekapt op 100%).

## Wat zie je op het dashboard?

- **Waterstand ≤ 100%:** Geen verschil — je ziet het normale volume en percentage.
- **Waterstand > 100%:** Je ziet nu het **werkelijke volume**, inclusief het water in het mangat.
  Het percentage kan boven de 100% uitkomen (bv. 105%), wat aangeeft dat het water in het mangat
  staat.

### Dwarsdoorsnede van een typische put

```
                    deksel
              ┌───────────────┐
              │    mangat     │         oppervlakte: 0,50 m²
              │    (smal)     │       ← hier wordt het volume
              │               │         apart berekend
              │               │
         ┌────┘- - - - - - - -└────┐  ← sensor "vol" niveau
         │                         │
         │      hoofdput           │    oppervlakte: 4,55 m²
         │      (breed)            │  ← hier geldt de
         │                         │    normale berekening
         │                         │
         │                         │
         │                         │
         │▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓│  ← onbruikbare bodem
         └─────────────────────────┘
```

## Veelgestelde vragen

**Moet ik dit instellen?**
Nee, het is optioneel.  Als je het veld leeg laat, werkt alles zoals voorheen.

**Wat als mijn put geen mangat heeft?**
Laat het veld leeg.  De compensatie wordt dan niet toegepast.

**Kan ik een waarde van 0 invullen?**
Ja, dat heeft hetzelfde effect als het veld leeg laten: het niveau wordt afgekapt op 100%.

**Werkt dit ook met druksensoren?**
Ja, de compensatie werkt voor zowel afstandssensoren (ultrasoon) als druksensoren.

**Hoe nauwkeurig moet de oppervlakte zijn?**
Een schatting is voldoende.  Het gaat om het verschil tussen de brede put en het smalle mangat —
zelfs een ruwe meting geeft al een veel betere volumeberekening dan geen compensatie.
