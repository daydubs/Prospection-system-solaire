using System;
using System.Collections.Generic;
using UnityEngine;

#region Time System

[Serializable]
public class GameDateTime
{
    [SerializeField] private int year = 2150;
    [SerializeField] private int month = 1;
    [SerializeField] private int day = 1;
    [SerializeField] private int hour = 8;
    [SerializeField] private int minute = 0;
    [SerializeField] private float secondFraction = 0f;

    public int Year => year;
    public int Month => month;
    public int Day => day;
    public int Hour => hour;
    public int Minute => minute;

    public static readonly string[] MonthNamesFr = {
        "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
        "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"
    };

    public static readonly int[] DaysInMonths = {
        31, 28, 31, 30, 31, 30,
        31, 31, 30, 31, 30, 31
    };

    public GameDateTime(int year = 2150, int month = 1, int day = 1, int hour = 8, int minute = 0)
    {
        this.year = Mathf.Max(2000, year);
        this.month = Mathf.Clamp(month, 1, 12);
        this.day = Mathf.Clamp(day, 1, GetDaysInCurrentMonth(this.year, this.month));
        this.hour = Mathf.Clamp(hour, 0, 23);
        this.minute = Mathf.Clamp(minute, 0, 59);
        this.secondFraction = 0f;
    }

    public static int GetDaysInCurrentMonth(int y, int m)
    {
        if (m < 1 || m > 12) return 30;
        if (m == 2 && IsLeapYear(y)) return 29;
        return DaysInMonths[m - 1];
    }

    public static bool IsLeapYear(int y)
    {
        return (y % 4 == 0 && y % 100 != 0) || (y % 400 == 0);
    }

    /// <summary>
    /// Advances in-game time by in-game seconds.
    /// Returns true if at least one day rolled over.
    /// </summary>
    public bool AdvanceSeconds(float inGameSeconds, out bool monthChanged, out bool yearChanged)
    {
        monthChanged = false;
        yearChanged = false;
        bool dayChanged = false;

        secondFraction += inGameSeconds;
        int wholeSeconds = (int)secondFraction;
        secondFraction -= wholeSeconds;

        if (wholeSeconds <= 0) return false;

        int totalMinutes = minute + (wholeSeconds / 60);
        int remSeconds = wholeSeconds % 60;
        secondFraction += remSeconds;

        minute = totalMinutes % 60;
        int totalHours = hour + (totalMinutes / 60);
        hour = totalHours % 24;

        int daysToAdd = totalHours / 24;
        while (daysToAdd > 0)
        {
            dayChanged = true;
            day++;
            int maxDays = GetDaysInCurrentMonth(year, month);
            if (day > maxDays)
            {
                day = 1;
                month++;
                monthChanged = true;
                if (month > 12)
                {
                    month = 1;
                    year++;
                    yearChanged = true;
                }
            }
            daysToAdd--;
        }

        return dayChanged;
    }

    public void AdvanceDays(int days)
    {
        for (int i = 0; i < days; i++)
        {
            day++;
            int maxDays = GetDaysInCurrentMonth(year, month);
            if (day > maxDays)
            {
                day = 1;
                month++;
                if (month > 12)
                {
                    month = 1;
                    year++;
                }
            }
        }
    }

    public string ToDateString()
    {
        string mName = (month >= 1 && month <= 12) ? MonthNamesFr[month - 1] : $"Mois {month}";
        return $"{day} {mName} {year}";
    }

    public string ToShortDateString()
    {
        return $"{day:D2}/{month:D2}/{year}";
    }

    public string ToTimeString()
    {
        return $"{hour:D2}:{minute:D2}";
    }

    public string ToFullDateTimeString()
    {
        return $"{ToDateString()} - {ToTimeString()}";
    }

    public GameDateTime Clone()
    {
        return new GameDateTime(year, month, day, hour, minute)
        {
            secondFraction = this.secondFraction
        };
    }
}

#endregion

#region Technology & Research

public enum TechCategory
{
    Propulsion,
    Extraction,
    Scanning,
    Energy,
    HullAndShield,
    Science
}

[Serializable]
public class TechnologyData
{
    public string id;
    public string displayName;
    public TechCategory category;
    [TextArea(2, 4)]
    public string description;
    public double researchCost;
    public float researchDurationDays;
    public bool isUnlocked;
    public List<string> prerequisites = new List<string>();

    public TechnologyData(string id, string name, TechCategory category, string desc, double cost, float durationDays = 1f, bool unlocked = false)
    {
        this.id = id;
        this.displayName = name;
        this.category = category;
        this.description = desc;
        this.researchCost = cost;
        this.researchDurationDays = durationDays;
        this.isUnlocked = unlocked;
        this.prerequisites = new List<string>();
    }
}

#endregion

#region Player Stats

[Serializable]
public class PlayerStats
{
    [Header("Identity & Profile")]
    public string pilotName = "Commandant Dany";
    public string corporationName = "Astraea Mining Corp";
    public string title = "Prospecteur Indépendant";
    public int difficultyLevel = 1; // 0 = Facile, 1 = Normal, 2 = Difficile
    public string difficultyName = "Normal";
    public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 1000;

    [Header("Skill Levels")]
    public int miningSkill = 1;       // Increases yield from asteroids & planets
    public int scanningSkill = 1;     // Reveals richer resource deposits
    public int pilotingSkill = 1;     // Reduces fuel consumption & increases speed
    public int tradingSkill = 1;      // Improves buying / selling margins

    [Header("Reputations (Faction Standings)")]
    public float repEarthCoalition = 10f;   // -100 to +100
    public float repMarsRepublic = 0f;
    public float repBeltAlliance = 5f;
    public float repJovianConsortium = 0f;

    [Header("Career Statistics")]
    public int totalBodiesVisited = 1;
    public int totalAsteroidsMined = 0;
    public double totalCreditsEarned = 0;
    public float totalCargoMinedTons = 0f;
    public float totalDistanceTraveledKm = 0f;

    public void AddXP(int amount)
    {
        currentXP += amount;
        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            level++;
            xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.5f);
            Debug.Log($"[PlayerStats] Level Up! Le joueur atteint le Niveau {level}!");
        }
    }
}

#endregion

#region Ship & Equipment

[Serializable]
public class CargoItem
{
    public string resourceId;
    public string resourceName;
    public float quantity;       // in Metric Tons (t)
    public double basePricePerTon;

    public CargoItem(string id, string name, float qty, double price)
    {
        this.resourceId = id;
        this.resourceName = name;
        this.quantity = qty;
        this.basePricePerTon = price;
    }
}

[Serializable]
public class ShipStats
{
    [Header("Vessel Identity")]
    public string shipName = "Astraea-I";
    public string shipClass = "Navette de Prospection Légère";

    [Header("Hull & Defense")]
    public float currentHull = 100f;
    public float maxHull = 100f;
    public float currentShield = 100f;
    public float maxShield = 100f;
    public float shieldRegenRate = 5f; // per second

    [Header("Propulsion & Energy")]
    public float currentFuel = 500f;
    public float maxFuel = 500f;
    public float fuelConsumptionRate = 1.0f; // per second in flight
    public float currentEnergy = 100f;
    public float maxEnergy = 100f;
    public float energyRechargeRate = 10f;

    [Header("Cargo Capacity")]
    public float maxCargoCapacity = 50f; // In metric tons
    public List<CargoItem> cargo = new List<CargoItem>();

    [Header("Equipment & Modifiers")]
    public float miningLaserPower = 10f;   // Mining speed / damage
    public float scannerRange = 250f;      // Sensor range in units
    public int warpDriveTier = 1;          // Warp engine capability
    public float cruiseSpeedModifier = 1.0f;

    public float CurrentCargoTons
    {
        get
        {
            float total = 0f;
            for (int i = 0; i < cargo.Count; i++)
            {
                if (cargo[i] != null) total += cargo[i].quantity;
            }
            return total;
        }
    }

    public float RemainingCargoSpace => Mathf.Max(0f, maxCargoCapacity - CurrentCargoTons);

    public bool AddCargo(string resId, string resName, float amount, double basePrice)
    {
        if (RemainingCargoSpace < amount) return false;

        CargoItem existing = cargo.Find(c => c.resourceId == resId);
        if (existing != null)
        {
            existing.quantity += amount;
        }
        else
        {
            cargo.Add(new CargoItem(resId, resName, amount, basePrice));
        }
        return true;
    }

    public bool RemoveCargo(string resId, float amount)
    {
        CargoItem existing = cargo.Find(c => c.resourceId == resId);
        if (existing == null || existing.quantity < amount) return false;

        existing.quantity -= amount;
        if (existing.quantity <= 0.0001f)
        {
            cargo.Remove(existing);
        }
        return true;
    }

    public float GetCargoAmount(string resId)
    {
        CargoItem existing = cargo.Find(c => c.resourceId == resId);
        return existing != null ? existing.quantity : 0f;
    }
}

#endregion

#region Save Data Container

[Serializable]
public class SaveGameData
{
    public string saveSlotName = "Sauvegarde";
    public string saveDateRealTime;
    public GameDateTime gameTime;
    public double credits;
    public List<string> unlockedTechIds = new List<string>();
    public PlayerStats playerStats;
    public ShipStats shipStats;
    public List<string> discoveredBodies = new List<string>();
}

#endregion
