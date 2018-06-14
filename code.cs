using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

class Point 
{
    public int X { get;set;}
    public int Y { get;set;}
    
    public Point (int x, int y)
    {
        X = x;
        Y = y;
    }
}

class Unit : Point
{   
    public int Id {get;set;}       
    public int Team {get;set;}
    public int AttackRange {get;set;}
    public int Health {get;set;}
    public int MaxHealth {get;set;}
    public int Damage {get;set;}
    public int MovementSpeed {get;set;}
    public int IsVisible {get;set;}
    
    public Unit(int x, int y) : base(x,y)
    {
       
    }
    
    public bool IsRanged
    {
        get 
        {
            return AttackRange >  150;
        }
    }
}



class Groot : Unit
{
    public Groot (int x, int y): base(x,y)
    {
    }
    
    public bool IsAgressive
    {
        get{return Health < MaxHealth;}   
    }
    
    public bool IsEnemy(IList<Hero> allyHeroes, IList<Unit> enemyHeroes)
    {
        if (!IsAgressive) return false;
        if (!enemyHeroes.Any()) return true;
        
        var minAllyDist = allyHeroes.Min(h => Player.GetSqrDistance(h, this));
        var minEnemyDist = enemyHeroes.Min(h => Player.GetSqrDistance(h, this));
        
        return minAllyDist <= minEnemyDist;
    }
}

class Hero : Unit
{
    
    public int GoldValue {get;set;}
    public int ItemsOwned {get;set;}
    public string HeroType {get;set;}
    
    public int CountDown1 {get;set;}
    public int CountDown2 {get;set;}
    public int CountDown3 {get;set;}
        
    public int Mana {get;set;}
    public int MaxMana {get;set;}
    public int StunDuration {get;set;}
    
    public Hero(int x, int y): base(x,y)
    {
           
    }    
   
}

class AggroThing : Unit
{
    public Unit AggroUnit {get;set;}
    public int AggroTimeLeft {get;set;}
    public int AggroTSet {get;set;}   
    
    public AggroThing(int x, int y): base(x,y)
    {
        
    }
}

class Creature : AggroThing
{
    public Creature(int x, int y): base(x,y)
    {
        
    }
}

class Tower : AggroThing
{
    
    public Tower(int x, int y): base(x,y)
    {
       
    }
}


class Vector 
{
    public Point StartPoint {get;set;}
    public Point EndPoint {get;set;}
    
    public void Mult (double coeff)
    {
    
        
        EndPoint.X = (int) (StartPoint.X + (EndPoint.X - StartPoint.X) * coeff);
        if (EndPoint.X > StartPoint.X) 
        {
          EndPoint.X--;   
        }
        else
        {
         EndPoint.X++;
        }
        
                
        EndPoint.Y = (int) (StartPoint.Y + (EndPoint.Y - StartPoint.Y) * coeff);
        if (EndPoint.Y > StartPoint.Y) 
        {
          EndPoint.Y--;   
        }
        else
        {
         EndPoint.Y++;
        }
    }
}

class Item
{
    public string ItemName {get;set;}
    
    public string ItemType {get;set;}
    
    
    public int ItemCost {get;set;}
    public int Damage {get;set;}
    public int Health {get;set;}
    public int MaxHealth {get;set;}
    public int Mana {get;set;}
    public int MaxMana {get;set;}
    public int MoveSpeed {get;set;}
    public int ManaRegeneration {get;set;}
    public int IsPotion {get;set;} 
    
}


class TargetUnitData
{
    public Unit TargetUnit {get;set;}
    public Point OneShootKillPoint {get;set;}
}

class AggroData
{
    public int HeroId {get;set;}
    public int TimeLeft {get;set;}
}


/**
 * Made with love by AntiSquid, Illedan and Wildum.
 * You can help children learn to code while you participate by donating to CoderDojo.
 **/
class Player
{
    private static int T = 0;
    
    private static IList<Item> Items;
    
    private static IDictionary<int, List<Item>> HeroItems = new Dictionary<int, List<Item>>();
    private static IDictionary<int, double> KeyItemParameterValues = new Dictionary<int, double>();
    private static IDictionary<int, int> MinItemCosts = new Dictionary<int, int>();
    private static IDictionary<int, bool> IsHeroSelling = new Dictionary<int, bool>();
    private static IDictionary<int, Unit> MyHeroTargets = new Dictionary<int, Unit>();
    
    private static IDictionary<int, AggroData> AggroDatas = new Dictionary<int, AggroData>();
    
    private static int OneShootUnitId = -1;
    private static int TwoShootsUnitId = -1;
   
    private static Tower MyTower;
    private static Tower EnemyTower;
    private static int Gold = 0;
    
    private const double HERO_ATTACK_TIME = 0.1;
    private const double UNIT_ATTACK_TIME = 0.2;
    private const int RANGED_HERO_RANGE = 150;
    private const int BEHIND_CREATURE_DIST = 50;
    private const int AGGROUNITRANGE = 300;
    private const int AGGROUNITRANGE2 = AGGROUNITRANGE * AGGROUNITRANGE;
    private const int AGGROUNITTIME = 3;
    
    private static IDictionary<int, IList<int>> Targets = new Dictionary<int,IList<int>>();
    private static IList<Groot> Groots = new List<Groot>();
    
    private static Unit PulledHero = null;
    private static Point PullPoint = null;
    
    static string BuyItem(Hero hero, Hero otherHero, IList<Unit> enemyUnits, int heroNumber)
    {
        if (TwoShootsUnitId != -1) return null;
        if (Targets.ContainsKey(hero.Id)) return null;
        
        if (hero.Health < hero.MaxHealth / 4d)
        {
            var healItems = Items.Where(i => i.IsPotion == 1 && i.Health > 0).OrderBy(i => i.Health).ToList();
            var healItemToBuy = healItems.LastOrDefault(i => i.ItemCost <= Gold);
            if (healItemToBuy != null)
            {
                Gold -= healItemToBuy.ItemCost;
                return "BUY " + healItemToBuy.ItemName + "; BUY HEAL";
            }
        }
        
        //if (enemyUnits.Any(u => u.AttackRange >= GetDistance(u, hero))) return null;
               
        
        var heroManaRegSumm = 0d;
        var otherHeroManaRegSumm = 0d;
        
        foreach (var item in HeroItems[hero.Id])
        {
            heroManaRegSumm += (item.ManaRegeneration);      
        }
        
        if (otherHero != null)
        {
            foreach (var item in HeroItems[otherHero.Id])
            {
                otherHeroManaRegSumm += (item.ManaRegeneration);      
            } 
        }        
        if (otherHero != null && (heroNumber == 0 && heroManaRegSumm > otherHeroManaRegSumm || heroNumber == 1 && heroManaRegSumm >= otherHeroManaRegSumm)) return null;
        
        var orderedItems = Items.OrderBy(i => i.ManaRegeneration).ToList();
       
        if (hero.ItemsOwned < 4)
        {
            var itemToBuy = orderedItems.LastOrDefault(
                x => x.ItemType != "Boots" &&
                x.ItemCost <= Gold && 
                x.ManaRegeneration > KeyItemParameterValues[hero.Id] &&
                x.ItemCost > MinItemCosts[hero.Id]);
            if (itemToBuy != null)
            {
                Gold -= itemToBuy.ItemCost;
                HeroItems[hero.Id].Add(itemToBuy);
                IsHeroSelling[hero.Id] = false;
                return "BUY " + itemToBuy.ItemName;
            }
        }
        
       
        var orderedByCostItems = HeroItems[hero.Id].OrderBy(i => i.ItemCost).ToList();
        for (int i = 0; i < orderedByCostItems.Count; ++i)
        {
            var currGold = Gold;
            var maxParamValue = KeyItemParameterValues[hero.Id];
            int maxCost = MinItemCosts[hero.Id];
            for (int j = 0; j <=i; ++j)
            {
            
                var sellingItem = orderedByCostItems[j];
                if (sellingItem.Health > 0) continue;
                currGold += sellingItem.ItemCost / 2;
                maxParamValue = Math.Max(sellingItem.ManaRegeneration, maxParamValue);
                maxCost = Math.Max(sellingItem.ItemCost, maxCost);
                
                var itemToBuy = orderedItems.LastOrDefault(
                    x => x.ItemType != "Boots" && 
                    x.ItemCost <= currGold && 
                    x.ManaRegeneration > maxParamValue && 
                    x.ItemCost > maxCost);
                    
                if (itemToBuy != null)
                {
                    KeyItemParameterValues[hero.Id] = maxParamValue;   
                    MinItemCosts[hero.Id] = maxCost;
                    
                    Gold += sellingItem.ItemCost / 2;
                    HeroItems[hero.Id].Remove(sellingItem);
                    IsHeroSelling[hero.Id] = true;
                    return "SELL " + sellingItem.ItemName;
                }
            }
        }
        
        return null;
    }
    
    public static int GetSqrDistance(Point point1, Point point2)
    {
        return (int)(Math.Pow(point1.X - point2.X, 2) + Math.Pow(point1.Y - point2.Y, 2));
    }
    
    static double GetDistance(Point point1, Point point2)
    {
        return Math.Sqrt(GetSqrDistance(point1, point2)) * 1d;
    }
    
    static double GetAttackTime(Point sourcePoint, Point targetPoint, Unit unit)
    {
        var attackTime = unit is Hero ?  HERO_ATTACK_TIME : UNIT_ATTACK_TIME;
        
        if (unit.AttackRange > RANGED_HERO_RANGE)
        {
            var dist = GetDistance(sourcePoint, targetPoint);
            attackTime += attackTime * dist * 1d / unit.AttackRange;            
        }
        
        return attackTime;
    }
    
    static Point GetMovingPoint(Unit sourceUnit, Point targetUnit, Point endVectorPoint)
    {                       
        var vector = new Vector 
        {
            StartPoint = new Point(targetUnit.X, targetUnit.Y),
            EndPoint = new Point (endVectorPoint.X, endVectorPoint.Y)
        };
       
        var dist = GetDistance(vector.StartPoint, vector.EndPoint);
        var coeff = (sourceUnit.AttackRange)*1d / dist;    

        vector.Mult(coeff);           
        return vector.EndPoint;
        
    }
    
    static string GetAbility(Hero hero, Hero otherHero, IList<Unit> enemyUnits, IList<Unit> myUnits, bool isCloseToEnemyTower)
    {        
        if (TwoShootsUnitId != -1) return null;
        if (hero.HeroType == "DOCTOR_STRANGE")
        {          
           
            if (isCloseToEnemyTower) return null;
            
            var enemyHeroes = enemyUnits.Where(u => u is Hero).ToList();
            var pullCommand = GetPullCommand(hero, otherHero, enemyHeroes, myUnits, enemyUnits);      
            if (pullCommand != null) return pullCommand;
            
            var allyHeroes = new List<Hero>{hero};
            if (otherHero != null) allyHeroes.Add(otherHero);
            var aoeHealCommand  = GetAoeHealCommand(hero, allyHeroes, enemyUnits);
            if (aoeHealCommand != null) return aoeHealCommand;            
            
            var shieldCommand  = GetShieldCommand(hero, allyHeroes, enemyHeroes);
            if (shieldCommand != null) return shieldCommand;
                        
            return null;
            //var pullCommand = GetPullCommand(hero, enemyHeroes);        
            //return pullCommand;
            
        }
        else if (hero.HeroType == "IRONMAN")
        {
            var enemyHeroes = enemyUnits.Where(u => u is Hero).ToList();
            
            var blinkCommand = GetBlinkCommand(hero, myUnits, enemyHeroes);
            if (blinkCommand != null) return blinkCommand;
            
            if (isCloseToEnemyTower) return null;
            var fireballCommand = GetFireballCommand(hero, otherHero, enemyUnits);
            if (fireballCommand != null) return fireballCommand;
            var burningCommand = GetBurningCommand (hero, enemyHeroes);
            return burningCommand;
            
        }
        else
        {
            throw new Exception("unknown hero type");   
        }
    }
    
    static string GetAoeHealCommand(Hero hero, IList<Hero> allyHeroes, IList<Unit> enemyUnits)
    {       
        if (hero.CountDown1 > 0) return null;           
        if (hero.Mana < 50 || hero.Mana < hero.MaxMana / 2d) return null;
     
        var isMyTowerClose = GetDistance(hero, MyTower) < 100;       
        if (!isMyTowerClose && Targets.ContainsKey(hero.Id)) return null;
               
        var healingAmount = hero.Mana * 0.2;
        
        var heroesToHeal = allyHeroes.Where(h => GetDistance(hero, h) <= 250 && healingAmount < h.MaxHealth - h.Health).ToList();
        var heroToHeal = heroesToHeal.OrderBy(h => h.Health).FirstOrDefault();
                
        if (heroToHeal != null)
            return "AOEHEAL " + heroToHeal.X + " " + heroToHeal.Y + ";AOEHEAL " + heroToHeal.X + " " + heroToHeal.Y ;
        return null;    
        
    }
    
    static string GetShieldCommand(Hero hero, IList<Hero> allyHeroes, IList<Unit> enemyHeroes)
    {
        if (hero.CountDown2 > 0) return null; 
        if (hero.Mana < 40) return null;
        
        
        var isMyTowerClose = GetDistance(hero, MyTower) < 100;   
        if (!isMyTowerClose && Targets.ContainsKey(hero.Id)) return null; //TODO: исользовать вдали от башни
        
        var heroesToShield = allyHeroes.Where(
            h => GetDistance(hero, h) <= 500 &&
            (h.Health < h.MaxHealth / 3d ||
            enemyHeroes.Any(eh => !eh.IsRanged && eh.AttackRange >= GetDistance(eh, h)))).ToList(); 
            
        var heroToShield = heroesToShield.OrderBy(h => h.Health).FirstOrDefault();
                
        if (heroToShield != null)
            return "SHIELD " + heroToShield.Id + ";SHIELD " + heroToShield.Id;
        return null;      
    }
    
    static string GetPullCommand(Hero hero, Hero otherHero, IList<Unit> enemyHeroes, IList<Unit> myUnits, IList<Unit> enemyUnits)
    {
        if (hero.CountDown3 > 0) return null;           
        if (hero.Mana < 40) return null;
        //if (Targets.ContainsKey(hero.Id)) return null;
        
        if (enemyUnits.Any(u => GetSqrDistance(MyTower, u) <= GetSqrDistance(MyTower, hero))) return null;
      
        var isMyTowerClose = GetDistance(hero, MyTower) < 100;  //TODO
               
               
        Unit okHero = null;
        var maxAllyCount = 0;
        Point resPullPoint = null;
        var maxHealth = 0;
        
        foreach (var h in enemyHeroes)
        {
            if (GetDistance(hero, h) > 400) continue;
            if (!hero.IsRanged && !isMyTowerClose) continue;
            
            var pullPoint = GetPullPoint(hero, h);
            var allyCount = myUnits.Where(
                u => u is Hero && GetMinAttackTime(u, pullPoint) < 1 ||
                u is Tower && GetDistance(u, pullPoint) <= u.AttackRange || 
                u is Creature && GetMinAttackTime(u, h) < 1 
                && GetDistance(u, pullPoint) < GetDistance(u, enemyUnits.OrderBy(uu => GetSqrDistance(u, uu)).First())
                ).Count();
                
            if (allyCount > maxAllyCount || allyCount == maxAllyCount && h.Health < maxHealth)
            {
                okHero = h;
                maxAllyCount = allyCount;
                resPullPoint = pullPoint;
                maxHealth = h.Health;
            }
            
        }
        
        if (maxAllyCount >= 3)
        {
            
            PulledHero = okHero;
            PullPoint = resPullPoint;
            Console.Error.WriteLine(PulledHero.Id);
            return "PULL " + okHero.Id + ";PULL " + okHero.Id;
        }
        return null;
        
        /*
        var nearestEnemy = enemyHeroes.OrderBy(x => GetDistance(x, hero)).FirstOrDefault(
            x => (x.IsRanged || isMyTowerClose) && GetDistance(x, hero) <= 400);
        if (nearestEnemy == null) return null; //невидимый
        
        return "PULL " + nearestEnemy.Id;
        */
    }
    
    static Point GetPullPoint(Hero hero, Unit targetUnit)
    {
        var dist = GetDistance(hero, targetUnit);
        var startPoint = new Point(targetUnit.X, targetUnit.Y);
        var endPoint = new Point(hero.X, hero.Y);
        if (dist <= 200) return endPoint;
        
        var vector = new Vector
        {
            StartPoint = startPoint,
            EndPoint = endPoint
        };
        
        var coeff = 200d / dist;
        vector.Mult(coeff);
        return vector.EndPoint;
    }
    
    static string GetBlinkCommand(Hero hero, IList<Unit> myUnits, IList<Unit> enemyHeroes)
    {
        if (hero.CountDown1 > 0) return null;
        if (hero.Mana < 16) return null;
        
        var nearestEnemy = enemyHeroes.OrderBy(x => GetDistance(x, hero)).FirstOrDefault();
        if (nearestEnemy == null) return null; //невидимый или в стане
        
        //Console.Error.WriteLine((nearestEnemy as Hero).StunDuration);
        
        if ((nearestEnemy as Hero).StunDuration > 0) return null;
        var hasFarCreatures = myUnits.Any(u => u is Creature && GetDistance(MyTower, u) > GetDistance(MyTower, nearestEnemy));
        if (nearestEnemy.IsRanged && hasFarCreatures) return null;
        
        var dist = GetDistance(hero, nearestEnemy);
        if (dist > 150) return null;
        
        return "BLINK " + MyTower.X + " " + hero.Y + ";BLINK " + MyTower.X + " " + hero.Y;
    }
    
    static string GetFireballCommand(Hero hero, Hero otherHero, IList<Unit> enemyUnits)
    {
        if (hero.CountDown2 > 0) return null;
        if (hero.Mana < 60 || hero.Mana < hero.MaxMana / 2d) return null;
        if (enemyUnits.Any(u => u.AttackRange >= GetDistance(u, hero))) return null;
        
        var isMyTowerClose = GetDistance(hero, MyTower) < 100;   
        if (!isMyTowerClose && Targets.ContainsKey(hero.Id)) return null;
        
        
        
        var enemyHeroes = enemyUnits.Where(u => u is Hero).ToList();
        
        var distTo2 = enemyHeroes.All(h => !h.IsRanged) ? 450 : 900;
        var distTo1 =  enemyHeroes.All(h => !h.IsRanged) ? 225 : 450;
        
        if (enemyHeroes.Count == 2 && GetDistance(enemyHeroes[0], enemyHeroes[1]) <= 50)
        {
            var fireballPoint = new Point((enemyHeroes[0].X + enemyHeroes[1].X)/2, (enemyHeroes[0].Y + enemyHeroes[1].Y)/2);
            if (GetDistance(hero, fireballPoint) < distTo2)
            {
                return "FIREBALL " + fireballPoint.X + " " + fireballPoint.Y +";fireball 2";
            }
        }
                
        var nearestEnemy = enemyHeroes.OrderBy(x => GetDistance(x, hero)).FirstOrDefault();
        if (nearestEnemy != null && GetDistance(hero, nearestEnemy) <= distTo1) return "FIREBALL " + nearestEnemy.X + " " + nearestEnemy.Y + ";fireball 1"; 
        
        if (enemyHeroes.All(h => !h.IsRanged)) return null;
       
        var groots = Groots.Where(u => !u.IsAgressive && 
            enemyHeroes.Any(h => GetSqrDistance(u, h) < GetSqrDistance(u, hero) && (otherHero == null || GetSqrDistance(u, h) < GetSqrDistance(u, otherHero)))).ToList();        
        var nearestGroot = groots.OrderBy(g => GetSqrDistance(g, hero)).FirstOrDefault();
        if (nearestGroot != null && GetDistance(nearestGroot, hero) <= 900) return "FIREBALL " + nearestGroot.X + " " + nearestGroot.Y + "; aggro GROOT"; 
        
        return null;
    }
    
    static string GetBurningCommand(Hero hero, IList<Unit> enemyHeroes)
    {
        if (hero.CountDown3 > 0) return null;
        if (hero.Mana < 50) return null;
        
        var isMyTowerClose = GetDistance(hero, MyTower) < 100;   
        if (!isMyTowerClose && Targets.ContainsKey(hero.Id) && Targets[hero.Id].Any(t => !enemyHeroes.Any(h => h.Id == t))) return null;
        //if (Targets.ContainsKey(hero.Id) && Targets[hero.Id].Any(t => !enemyHeroes.Any(h => h.Id == t))) return null;
                
        var nearestEnemy = enemyHeroes.OrderBy(x => GetDistance(x, hero)).FirstOrDefault();
        if (nearestEnemy == null) return null; //невидимый
        var dist = GetDistance(hero, nearestEnemy);
        if (dist > 250) return null;
        
        return "BURNING " + nearestEnemy.X + " " + nearestEnemy.Y + ";BURNING " + nearestEnemy.X + " " + nearestEnemy.Y;
    }
    
    static void Main(string[] args)
    {
        string[] inputs;
        int myTeam = int.Parse(Console.ReadLine());
        int bushAndSpawnPointCount = int.Parse(Console.ReadLine()); // usefrul from wood1, represents the number of bushes and the number of places where neutral units can spawn
        for (int i = 0; i < bushAndSpawnPointCount; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            string entityType = inputs[0]; // BUSH, from wood1 it can also be SPAWN
            int x = int.Parse(inputs[1]);
            int y = int.Parse(inputs[2]);
            int radius = int.Parse(inputs[3]);
        }
        
        int itemCount = int.Parse(Console.ReadLine()); // useful from wood2
        Items = new List<Item>();
        
        for (int i = 0; i < itemCount; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            string itemName = inputs[0]; // contains keywords such as BRONZE, SILVER and BLADE, BOOTS connected by "_" to help you sort easier
            int itemCost = int.Parse(inputs[1]); // BRONZE items have lowest cost, the most expensive items are LEGENDARY
            int damage = int.Parse(inputs[2]); // keyword BLADE is present if the most important item stat is damage
            int health = int.Parse(inputs[3]);
            int maxHealth = int.Parse(inputs[4]);
            int mana = int.Parse(inputs[5]);
            int maxMana = int.Parse(inputs[6]);
            int moveSpeed = int.Parse(inputs[7]); // keyword BOOTS is present if the most important item stat is moveSpeed
            int manaRegeneration = int.Parse(inputs[8]);
            int isPotion = int.Parse(inputs[9]); // 0 if it's not instantly consumed
            
            
            var splits = itemName.Split('_');
            var itemType = splits.Count() == 3 ? splits[1] : splits[0];
            
            Items.Add(new Item
            {
               ItemName = itemName,
               ItemType = itemType,
               ItemCost = itemCost,
               Damage = damage,
               Health = health,
               MaxHealth = maxHealth,
               Mana = mana,
               MaxMana = maxMana,
               MoveSpeed = moveSpeed,
               ManaRegeneration = manaRegeneration,
               IsPotion = isPotion
            });
        }       
        
                
        foreach (var item in Items)
        {
            Console.Error.WriteLine(
                item.ItemName + " " + item.ItemCost + " " + item.ManaRegeneration);
        }

        // game loop
        while (true)
        {
            PulledHero = null;
            PullPoint = null;
            
            Groots.Clear();
            T++;
            OneShootUnitId = -1; 
            TwoShootsUnitId = -1;
            var keysToRemove = new List<int>();
            foreach (var unitId in AggroDatas.Keys)
            {
                AggroDatas[unitId].TimeLeft--;     
                if (AggroDatas[unitId].TimeLeft == 0) keysToRemove.Add(unitId);
            }
            foreach (var key in keysToRemove)
            {
                AggroDatas.Remove(key);   
            }           
           
            
            int gold = int.Parse(Console.ReadLine());
            Gold = gold;
            
            int enemyGold = int.Parse(Console.ReadLine());
            int roundType = int.Parse(Console.ReadLine()); // a positive value will show the number of heroes that await a command
            
            int entityCount = int.Parse(Console.ReadLine());
            
            Hero doctorStrange = null;
            Hero ironman = null;
           
            var enemyUnits = new List<Unit>();     
            var myUnits = new List<Unit>();   
            
            for (int i = 0; i < entityCount; i++)
            {       
                
                inputs = Console.ReadLine().Split(' ');
                int unitId = int.Parse(inputs[0]);
                int team = int.Parse(inputs[1]);
                string unitType = inputs[2]; // UNIT, HERO, TOWER, can also be GROOT from wood1
                int x = int.Parse(inputs[3]);
                int y = int.Parse(inputs[4]);
                int attackRange = int.Parse(inputs[5]);
                int health = int.Parse(inputs[6]);
                int maxHealth = int.Parse(inputs[7]);
                int shield = int.Parse(inputs[8]); // useful in bronze
                int attackDamage = int.Parse(inputs[9]);
                int movementSpeed = int.Parse(inputs[10]);
                int stunDuration = int.Parse(inputs[11]); // useful in bronze
                int goldValue = int.Parse(inputs[12]);
                int countDown1 = int.Parse(inputs[13]); // all countDown and mana variables are useful starting in bronze
                int countDown2 = int.Parse(inputs[14]);
                int countDown3 = int.Parse(inputs[15]);
                int mana = int.Parse(inputs[16]);
                int maxMana = int.Parse(inputs[17]);
                int manaRegeneration = int.Parse(inputs[18]);
                string heroType = inputs[19]; // DEADPOOL, VALKYRIE, DOCTOR_STRANGE, HULK, IRONMAN
                int isVisible = int.Parse(inputs[20]); // 0 if it isn't
                int itemsOwned = int.Parse(inputs[21]); // useful from wood1
                
                
                if (unitType == "GROOT")
                {
                    var groot = 
                        new Groot(x,y){
                            Id = unitId,
                            Team = team,
                            AttackRange = attackRange, 
                            Health = health,
                            MaxHealth = maxHealth,
                            Damage = attackDamage,
                            MovementSpeed = movementSpeed,
                            IsVisible = isVisible,
                             };  
                    Groots.Add(groot);                    
                }
                else if (unitType == "UNIT")
                {
                    var creature = 
                        new Creature(x,y){
                            Id = unitId,
                            Team = team,
                            AttackRange = attackRange, 
                            Health = health,
                            MaxHealth = maxHealth,
                            Damage = attackDamage,
                            MovementSpeed = movementSpeed,
                            IsVisible = isVisible,
                             };  
                    if (team == myTeam)
                    {
                        myUnits.Add(creature);   
                    }
                    else
                    {
                        enemyUnits.Add(creature);   
                    }
                            
                }
                else if (unitType == "HERO")
                {
                    var hero = new Hero(x,y){
                            Id = unitId, 
                            Team = team,
                            AttackRange = attackRange, 
                            MovementSpeed = movementSpeed,
                            GoldValue = goldValue,
                            ItemsOwned = itemsOwned,
                            HeroType = heroType,
                            CountDown1 = countDown1,
                            CountDown2 = countDown2,
                            CountDown3 = countDown3,
                            Mana = mana,
                            Health = health,
                            MaxHealth = maxHealth,
                            MaxMana = maxMana,
                            Damage = attackDamage,
                            StunDuration = stunDuration,
                            IsVisible = isVisible,
                            };     
                            
                    if (team == myTeam)
                    {
                        myUnits.Add(hero);   
                        if (heroType == "IRONMAN")
                            ironman = hero;
                        else if (heroType == "DOCTOR_STRANGE")
                            doctorStrange = hero;
                        else
                            throw new Exception("unkown hero");
                            
                        if (!HeroItems.ContainsKey(hero.Id))
                        {
                            HeroItems.Add(hero.Id, new List<Item>());  
                            MinItemCosts.Add(hero.Id, 0);
                            KeyItemParameterValues.Add(hero.Id, 1);
                            IsHeroSelling.Add(hero.Id, false);                            
                        }
                    }
                    else
                    {
                        enemyUnits.Add(hero);   
                    }
                } 
                
                else if (unitType == "TOWER")
                {
                    var tower = new Tower(x,y){
                            Id = unitId, 
                            Team = team,
                            AttackRange = attackRange, 
                            Health = health,
                            MaxHealth = maxHealth,
                            Damage = attackDamage,
                            IsVisible = isVisible,
                            };
                            
                    if (team == myTeam)
                    {
                        myUnits.Add(tower);   
                        MyTower  = tower;
                    }
                    else
                    {
                        enemyUnits.Add(tower);   
                        EnemyTower = tower;     
                    }
                    
                                       
                      
                }                    
                
                
            }
            
            Targets.Clear();
            foreach (var unit in myUnits)
            {
                var target = GetUnitTarget(unit, enemyUnits);            
                if (target != null) 
                {
                    if (!Targets.ContainsKey(target.Id))
                        Targets.Add(target.Id, new List<int>());
                    Targets[target.Id].Add(unit.Id);
                }
            }
            foreach (var unit in enemyUnits)
            {
                var target = GetUnitTarget(unit, myUnits);
                if (target != null) 
                {
                    if (!Targets.ContainsKey(target.Id))
                        Targets.Add(target.Id, new List<int>());
                    Targets[target.Id].Add(unit.Id);
                }
            }            
            var allUnits = new List<Unit>();
            allUnits.AddRange(myUnits);
            allUnits.AddRange(enemyUnits);
            
            foreach (var groot in Groots)
            {
                var target = GetUnitTarget(groot, allUnits);
                if (target != null) 
                {
                    if (!Targets.ContainsKey(target.Id))
                        Targets.Add(target.Id, new List<int>());
                    Targets[target.Id].Add(groot.Id);
                }
                if (groot.IsAgressive) 
                    enemyUnits.Add(groot);
            }
            
            //if (Targets.ContainsKey(5)) Console.Error.WriteLine(Targets[5].Count);

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");


            // If roundType has a negative value then you need to output a Hero name, such as "DEADPOOL" or "VALKYRIE".
            // Else you need to output roundType number of any valid action, such as "WAIT" or "ATTACK unitId"
            if (roundType == -2)
            {
                Console.WriteLine("IRONMAN");                
                continue;
            }
            else if (roundType == -1)
            {
                Console.WriteLine("DOCTOR_STRANGE");
                continue;
            } 
            
            var ironmanAction = "";  
            var doctorStrangeAction = "";
            if (ironman != null)
                ironmanAction = MakeAction(ironman, doctorStrange, enemyUnits, myUnits, 0);
            if (doctorStrange != null)
                doctorStrangeAction = MakeAction(doctorStrange, ironman, enemyUnits, myUnits, 1);
                
            
            if (ironman != null && doctorStrange != null && doctorStrangeAction.Substring(0, 4) == "PULL")
            {
                if (PulledHero != null && GetMinAttackTime(ironman, PullPoint) <= 1)
                {
                    ironmanAction = "ATTACK " + PulledHero.Id + "; attack pulled";
                }       
            }
            
            if (ironman != null)
                Console.WriteLine(ironmanAction);
            if (doctorStrange != null)
                Console.WriteLine(doctorStrangeAction);
        }
    }
    
    static Unit GetUnitTarget(Unit unit, IList<Unit> enemyUnits)
    {
        if (unit is Tower)
        {
            var tower = unit as Tower;
            if(tower.AggroUnit != null && tower.AggroTimeLeft > 0 && GetDistance(tower, tower.AggroUnit) <= tower.AttackRange){
                tower.AggroTimeLeft--;
                return tower.AggroUnit;                
            }
            tower.AggroTSet = 1;
            tower.AggroUnit = null;
            tower.AggroTimeLeft = -1;           
            
            var nearestCreature = enemyUnits.Where(u => u is Creature).OrderBy(u => GetDistance(unit, u)).FirstOrDefault();
            if (nearestCreature != null && GetDistance(unit, nearestCreature) <= unit.AttackRange) return nearestCreature;
            
            var nearestHero = enemyUnits.Where(u => u is Hero).OrderBy(u => GetDistance(unit, u)).FirstOrDefault();
            if (nearestHero != null && GetDistance(unit, nearestHero) <= unit.AttackRange) return nearestHero;
            
            return null;
        }
        else if (unit is Creature)
        {
            var creature = unit as Creature;
            if (creature.AggroUnit != null && creature.AggroTimeLeft > 0 && GetSqrDistance(creature, creature.AggroUnit) <= AGGROUNITRANGE2 && creature.AggroUnit.IsVisible == 1)
            {
                if (GetMinAttackTime(creature, creature.AggroUnit) <= 1) return creature.AggroUnit;
                return null;
            }            
            
            var canAttackEnemies = enemyUnits.Where(u => 
                Math.Min(GetOneShootAttackTime(unit, u, new Point(unit.X, unit.Y)),GetOneShootAttackTime(unit, u, GetMovingPoint(unit, u, unit))) < 1).ToList();
            if (!canAttackEnemies.Any()) return null;
                        
            var minDist = canAttackEnemies.Min(e => GetSqrDistance(unit, e));
            var closestEnemies = enemyUnits.Where(u => GetSqrDistance(unit, u) == minDist).ToList();
            if (closestEnemies.Count == 1) return closestEnemies[0];
            var minHp = closestEnemies.Min(e => e.Health);
            var minHpEnemies = closestEnemies.Where(e => e.Health == minHp).ToList();
            if (minHpEnemies.Count == 1) return minHpEnemies[0];
            var minY = minHpEnemies.Min(e => e.Y);
            var minYEnemies = minHpEnemies.Where(e => e.Y == minY).ToList();
            if (minYEnemies.Count == 1) return minYEnemies[0];
            return minYEnemies.OrderBy(e => e.Id).First();   
                    
            
        }
        else if (unit is Hero) //TODO: здесь хрень
        {
            var canAttackEnemyHeroes = enemyUnits.Where(u => u is Hero && GetMinAttackTime(unit, u) < 1).ToList();
            if (!canAttackEnemyHeroes.Any()) return null;
            
            return canAttackEnemyHeroes.OrderBy(h => h.Health).First();
            
            /*
            var enemyHeroes = enemyUnits.Where(u => u is Hero).ToList();
            var nearestEnemyHeroe = enemyHeroes.OrderBy(h => 
                Math.Min(GetOneShootAttackTime(unit, h, new Point(unit.X, unit.Y)), GetOneShootAttackTime(unit as Hero, h, GetMovingPoint(unit as Hero, h, unit as Hero)))).FirstOrDefault();
            if (nearestEnemyHeroe != null && 
                Math.Min(GetOneShootAttackTime(unit, nearestEnemyHeroe, new Point(unit.X, unit.Y)),GetOneShootAttackTime(unit as Hero, nearestEnemyHeroe, GetMovingPoint(unit as Hero, nearestEnemyHeroe, unit as Hero))) < 1)
                return nearestEnemyHeroe;
            
            var weakestEnemy = enemyUnits.Where(
                u => !(u is Hero) &&  
                Math.Min(GetOneShootAttackTime(unit, u, new Point(unit.X, unit.Y)),GetOneShootAttackTime(unit as Hero, u, GetMovingPoint(unit as Hero, u, unit as Hero))) < 1).OrderBy(u => u.Health).FirstOrDefault();
                
            return weakestEnemy;
            */          
           
        }
        else if (unit is Groot) 
        {
            if (!(unit as Groot).IsAgressive) return null;
            var canAttackEnemyHeroes = enemyUnits.Where(u => u is Hero && GetMinAttackTime(unit, u) < 1).ToList();
            if (!canAttackEnemyHeroes.Any()) return null;
            
            return canAttackEnemyHeroes.OrderBy(h => GetSqrDistance(unit, h)).First();
        }
        else 
            throw new ArgumentException("Unknow unit type");
    }
        
        
    static string MakeAction(Hero myHero, Hero otherHero, IList<Unit> enemyUnits, IList<Unit> myUnits, int heroNumber)
    {           
        var isEnemyTowerTarget = Targets.ContainsKey(myHero.Id) && Targets[myHero.Id].Contains(EnemyTower.Id); 
        var isCloseToEnemyTower = GetDistance(EnemyTower, myHero) <= EnemyTower.AttackRange;
        var targetUnitData = GetTargetUnitData(myHero, otherHero, myUnits, enemyUnits, heroNumber);
                
        if (targetUnitData.OneShootKillPoint == null && PulledHero == null)
        {
            //Console.Error.WriteLine(myHero.HeroType);
            //Console.Error.WriteLine(myHero.Id);
            var ability = GetAbility(myHero, otherHero, enemyUnits, myUnits, isCloseToEnemyTower);
            if (ability != null)
            {
                //Console.WriteLine(ability);
                return ability;
            }  
            
            if (!isCloseToEnemyTower)
            {
                var itemToBuy = BuyItem(myHero, otherHero, enemyUnits, heroNumber);            
                if (itemToBuy != null)
                {
                    //Console.WriteLine(itemToBuy + "; buy" + itemToBuy); 
                    return itemToBuy + "; buy" + itemToBuy;
                }   
            }
        }
        
        
        //Console.WriteLine(GetMoveAttackCommand(myHero, otherHero, myUnits, enemyUnits, heroNumber, isEnemyTowerTarget, targetUnitData));
        return GetMoveAttackCommand(myHero, otherHero, myUnits, enemyUnits, heroNumber, isEnemyTowerTarget, targetUnitData);
       
    }   
    
    static double GetMinAttackTime(Unit sourceUnit, Point targetUnitPoint)
    {
        return Math.Min(GetOneShootAttackTime(sourceUnit, targetUnitPoint, new Point(sourceUnit.X, sourceUnit.Y)), 
            GetOneShootAttackTime(sourceUnit, targetUnitPoint, GetMovingPoint(sourceUnit, targetUnitPoint, sourceUnit)));
    }
    
   
    static double GetOneShootAttackTime(Unit sourceUnit, Point targetUnit, Point attackPoint)
    {
        //var distToTargetUnit = GetDistance(hero, targetUnit);   
        //var movingPoint = distToTargetUnit <= hero.AttackRange ? new Point(hero.X, hero.Y) : GetMovingPoint(hero, targetUnit); 
        if (GetDistance(attackPoint, targetUnit) > sourceUnit.AttackRange) return 9999;
        
        var distToMovingPoint = GetDistance(sourceUnit, attackPoint);
        var time = distToMovingPoint / sourceUnit.MovementSpeed;
        var attackTime = GetAttackTime(attackPoint, targetUnit, sourceUnit);
        return time + attackTime;
    }    
    
    
    static TargetUnitData GetTargetUnitData(Hero myHero, Hero otherHero, IList<Unit> myUnits, IList<Unit> enemyUnits, int heroNumber)
    {     
        var allyHeroes = new List<Hero>{myHero};
        if (otherHero != null) allyHeroes.Add(otherHero);
        var enemyHeroes = enemyUnits.Where(u => u is Hero).ToList();  
        
        
        
        if (TwoShootsUnitId != -1)
        {
            Point killPoint = null;
            var targetUnit = enemyUnits.Single(u => u.Id == TwoShootsUnitId);
            var attackTime = GetOneShootAttackTime(myHero, targetUnit, GetMovingPoint(myHero, targetUnit, myHero));
            if (attackTime < 1 && !enemyHeroes.Any(h => h.Damage >= targetUnit.Health && 
                Math.Min(GetOneShootAttackTime(h as Hero, targetUnit, new Point(h.X, h.Y)), GetOneShootAttackTime(h as Hero, targetUnit, GetMovingPoint(h as Hero, targetUnit, h))) < attackTime))
            {
                killPoint =  GetMovingPoint(myHero, targetUnit, myHero);
            }
            else
            {
                attackTime = GetOneShootAttackTime(myHero, targetUnit, new Point(myHero.X, myHero.Y));
                if (attackTime < 1 && !enemyHeroes.Any(h => h.Damage >= targetUnit.Health && 
                    Math.Min(GetOneShootAttackTime(h as Hero, targetUnit, new Point(h.X, h.Y)), GetOneShootAttackTime(h as Hero, targetUnit, GetMovingPoint(h as Hero, targetUnit, h))) < attackTime))
                {
                    killPoint =  new Point(myHero.X, myHero.Y);
                }
            }
            if (killPoint == null) throw new Exception("no kill point");
            return new TargetUnitData
            {
                TargetUnit =   targetUnit,
                OneShootKillPoint = killPoint
            };
        }
        
        var oneShootEnemyUnits = new List<Unit>();
        
        
        foreach (var unit in enemyUnits)
        {                  
            
            var moveAttackTime = GetOneShootAttackTime(myHero, unit, GetMovingPoint(myHero, unit, MyTower));
            if (moveAttackTime > 1) continue;      
            if (unit.Id == OneShootUnitId) continue;   
            
            
            var health = unit.Health;
            if (Targets.ContainsKey(unit.Id))
            {
                var myAttackingUnits = myUnits.Where(u => Targets[unit.Id].Any(x => x == u.Id)).ToList();
                var myAttackingUnitsBefore = myAttackingUnits.Where(u => GetMinAttackTime(u, unit) < moveAttackTime).ToList();
                foreach (var u in myAttackingUnitsBefore)
                {
                    health -= u.Damage;   
                }
            }
            
            if (health <= 0) continue;
            if (health > myHero.Damage) continue;              
                    
            if (unit is Creature || unit is Groot)
            {
                var hasCloseHeroes = enemyUnits.Any(h => h is Hero && h.Damage >= health && 
                    Math.Min(GetOneShootAttackTime(h as Hero, unit, new Point(h.X, h.Y)), GetOneShootAttackTime(h as Hero, unit, GetMovingPoint(h as Hero, unit, h))) < moveAttackTime);
                if (hasCloseHeroes) continue;   
            }
            
            oneShootEnemyUnits.Add(unit);            
        }
        
        if (oneShootEnemyUnits.Any()) 
        {            
            var targetUnit = oneShootEnemyUnits.OrderBy(x => GetDistance(myHero,x)).First();
            OneShootUnitId = targetUnit.Id;
            return new TargetUnitData 
            {
                TargetUnit = targetUnit,
                OneShootKillPoint = GetMovingPoint(myHero, targetUnit, MyTower)
            };
        }
        
        
        
        //if (!Targets.ContainsKey(myHero.Id) || !Targets[myHero.Id].Any(t => enemyHeroes.Any(h => h.Id == t)))          
        //{ 
            foreach (var unit in enemyUnits)
            {            
                var moveAttackTime = GetOneShootAttackTime(myHero, unit, new Point(myHero.X, myHero.Y));            
                if (moveAttackTime > 1) continue;              
                
                if (unit.Id == OneShootUnitId) continue;   
                
                var health = unit.Health;
                if (Targets.ContainsKey(unit.Id))
                {
                    var myAttackingUnits = myUnits.Where(u => Targets[unit.Id].Any(x => x == u.Id)).ToList();
                    var myAttackingUnitsBefore = myAttackingUnits.Where(u => GetMinAttackTime(u, unit) < moveAttackTime).ToList();
                    foreach (var u in myAttackingUnitsBefore)
                    {
                        
                        health -= u.Damage;   
                    }
                }
                
                if (health <= 0) continue;
                if (health > myHero.Damage) continue;   
                             
                if (unit is Creature || unit is Groot)
                {           
                    var hasCloseHeroes = enemyUnits.Any(h => h is Hero && h.Damage >= health && 
                         Math.Min(GetOneShootAttackTime(h as Hero, unit, new Point(h.X, h.Y)), GetOneShootAttackTime(h as Hero, unit, GetMovingPoint(h as Hero, unit, h))) < moveAttackTime);
                    if (hasCloseHeroes) continue;   
                }
                
                oneShootEnemyUnits.Add(unit);            
            }
            
            if (oneShootEnemyUnits.Any()) 
            {            
                var targetUnit = oneShootEnemyUnits.OrderBy(x => GetDistance(myHero,x)).First();
                OneShootUnitId = targetUnit.Id;
                return new TargetUnitData 
                {
                    TargetUnit = targetUnit,
                    OneShootKillPoint = new Point(myHero.X, myHero.Y)
                };
            }
        //}
        
        foreach (var unit in myUnits.Where(u => u is Creature))
        {          
            var moveAttackTime = GetOneShootAttackTime(myHero, unit, GetMovingPoint(myHero, unit, MyTower));              
            
            if (moveAttackTime > 1) continue;     
            
            if (unit.Id == OneShootUnitId) continue; 
            
            var health = unit.Health;
            if (Targets.ContainsKey(unit.Id))
            {
                var enemyAttackingUnits = enemyUnits.Where(u => Targets[unit.Id].Any(x => x == u.Id)).ToList();
                var enemyAttackingUnitsBefore = enemyAttackingUnits.Where(u => GetMinAttackTime(u, unit) < moveAttackTime).ToList();
                foreach (var u in enemyAttackingUnitsBefore)
                {
                    
                    health -= u.Damage;   
                }
            }
            
            if (health <= 0) continue;
            if (health > myHero.Damage) continue;   
                       
            var hasCloseHeroes = enemyUnits.Any(h => h is Hero && h.Damage >= health && 
                 Math.Min(GetOneShootAttackTime(h as Hero, unit, new Point(h.X, h.Y)), GetOneShootAttackTime(h as Hero, unit, GetMovingPoint(h as Hero, unit, h))) < moveAttackTime);
            if (hasCloseHeroes) continue; 
            
            oneShootEnemyUnits.Add(unit);            
        }
        
        if (oneShootEnemyUnits.Any()) 
        {            
            var targetUnit = oneShootEnemyUnits.OrderBy(x => GetDistance(myHero,x)).First();
            OneShootUnitId = targetUnit.Id;
            return new TargetUnitData 
            {
                TargetUnit = targetUnit,
                OneShootKillPoint = GetMovingPoint(myHero, targetUnit, MyTower)
            };
        }
       
        if (!Targets.ContainsKey(myHero.Id))          
        {  
            
            foreach (var unit in myUnits.Where(u => u is Creature))
            {          
                var moveAttackTime = GetOneShootAttackTime(myHero, unit, new Point(myHero.X, myHero.Y));              
                
                if (moveAttackTime > 1) continue;     
                
                if (unit.Id == OneShootUnitId) continue; 
                
                var health = unit.Health;
                if (Targets.ContainsKey(unit.Id))
                {
                    var enemyAttackingUnits = enemyUnits.Where(u => Targets[unit.Id].Any(x => x == u.Id)).ToList();
                    var enemyAttackingUnitsBefore = enemyAttackingUnits.Where(u => GetMinAttackTime(u, unit) < moveAttackTime).ToList();
                    foreach (var u in enemyAttackingUnitsBefore)
                    {
                        
                        health -= u.Damage;   
                    }
                }
                
                if (health <= 0) continue;
                if (health > myHero.Damage) continue;   
                           
                var hasCloseHeroes = enemyUnits.Any(h => h is Hero && h.Damage >= health && 
                     Math.Min(GetOneShootAttackTime(h as Hero, unit, new Point(h.X, h.Y)), GetOneShootAttackTime(h as Hero, unit, GetMovingPoint(h as Hero, unit, h))) < moveAttackTime);
                if (hasCloseHeroes) continue; 
                
                oneShootEnemyUnits.Add(unit);            
            }
            
            if (oneShootEnemyUnits.Any()) 
            {            
                var targetUnit = oneShootEnemyUnits.OrderBy(x => GetDistance(myHero,x)).First();
                OneShootUnitId = targetUnit.Id;
                return new TargetUnitData 
                {
                    TargetUnit = targetUnit,
                    OneShootKillPoint = new Point(myHero.X, myHero.Y)
                };
            }
        }
        
       
        //var isTarget = Targets.ContainsKey(myHero.Id) && Targets[myHero.Id].Any(t => !enemyHeroes.Any(h => h.Id == t));
        //var isOtherTarget =  otherHero != null && Targets.ContainsKey(otherHero.Id) && Targets[otherHero.Id].Any(t => !enemyHeroes.Any(h => h.Id == t));
        if (otherHero != null && heroNumber == 0)
        {            
            var twoHeroesKillUnits = new List<Unit>();
            foreach (var unit in enemyUnits)
            {
                var moveAttackTime = Math.Max(GetOneShootAttackTime(myHero, unit, GetMovingPoint(myHero, unit, MyTower)), GetMinAttackTime(otherHero, unit));       
                if (moveAttackTime > 1) continue;     
            
                if (unit.Id == OneShootUnitId) continue; 
                var health = unit.Health;
                
                if (Targets.ContainsKey(unit.Id))
                {
                    var myAttackingUnits = myUnits.Where(u => Targets[unit.Id].Any(x => x == u.Id)).ToList();
                    var myAttackingUnitsBefore = myAttackingUnits.Where(u => GetMinAttackTime(u, unit) < moveAttackTime).ToList();
                    foreach (var u in myAttackingUnitsBefore)
                    {
                        
                        health -= u.Damage;   
                    }
                }
                
                if (health <= 0) continue;
                if (health > myHero.Damage + otherHero.Damage) continue;   
                             
                if (unit is Creature || unit is Groot)
                {           
                    var hasCloseHeroes = enemyUnits.Any(h => h is Hero && h.Damage >= health && 
                         Math.Min(GetOneShootAttackTime(h as Hero, unit, new Point(h.X, h.Y)), GetOneShootAttackTime(h as Hero, unit, GetMovingPoint(h as Hero, unit, h))) < moveAttackTime);
                    if (hasCloseHeroes) continue;   
                }
                
                twoHeroesKillUnits.Add(unit);                
            }
            
            if (twoHeroesKillUnits.Any()) 
            {            
                var targetUnit = twoHeroesKillUnits.OrderBy(x => GetDistance(MyTower,x)).First();
                TwoShootsUnitId = targetUnit.Id;
                return new TargetUnitData 
                {
                    TargetUnit = targetUnit,
                    OneShootKillPoint = GetMovingPoint(myHero, targetUnit, MyTower)
                };
            }
            
            
            //if (!Targets.ContainsKey(myHero.Id) || !Targets[myHero.Id].Any(t => enemyHeroes.Any(h => h.Id == t)))            
            //{ 
                foreach (var unit in enemyUnits)
                {
                    var moveAttackTime = Math.Max(GetOneShootAttackTime(myHero, unit, new Point(myHero.X, myHero.Y)), GetMinAttackTime(otherHero, unit)); 
                    if (moveAttackTime > 1) continue;     
                
                    if (unit.Id == OneShootUnitId) continue; 
                    var health = unit.Health;
                    
                    if (Targets.ContainsKey(unit.Id))
                    {
                        var myAttackingUnits = myUnits.Where(u => Targets[unit.Id].Any(x => x == u.Id)).ToList();
                        var myAttackingUnitsBefore = myAttackingUnits.Where(u => GetMinAttackTime(u, unit) < moveAttackTime).ToList();
                        foreach (var u in myAttackingUnitsBefore)
                        {                        
                            health -= u.Damage;   
                        }
                    }
                    
                    if (health <= 0) continue;
                    if (health > myHero.Damage + otherHero.Damage) continue;   
                                 
                    if (unit is Creature || unit is Groot)
                    {           
                        var hasCloseHeroes = enemyUnits.Any(h => h is Hero && h.Damage >= health && 
                             Math.Min(GetOneShootAttackTime(h as Hero, unit, new Point(h.X, h.Y)), GetOneShootAttackTime(h as Hero, unit, GetMovingPoint(h as Hero, unit, h))) < moveAttackTime);
                        if (hasCloseHeroes) continue;   
                    }
                    
                    twoHeroesKillUnits.Add(unit);                
                }
                
                if (twoHeroesKillUnits.Any()) 
                {            
                    var targetUnit = twoHeroesKillUnits.OrderBy(x => GetDistance(MyTower,x)).First();
                    TwoShootsUnitId = targetUnit.Id;
                    return new TargetUnitData 
                    {
                        TargetUnit = targetUnit,
                        OneShootKillPoint = new Point(myHero.X, myHero.Y)
                    };
                }       
                
            //}
        }    
        
        var isEnemyTowerClose = GetDistance(myHero, EnemyTower) <= EnemyTower.AttackRange;
        
       
        var targetUnit2 = enemyUnits.Where(                
            
                u => 
                (u as Groot == null || Math.Min(GetOneShootAttackTime(myHero, u, new Point(myHero.X, myHero.Y)), GetOneShootAttackTime(myHero, u, GetMovingPoint(myHero, u, MyTower))) < 1) &&
                //((u as Groot == null) || (u as Groot).IsEnemy(allyHeroes, enemyHeroes)) &&                
                u.Id != OneShootUnitId &&
                (!isEnemyTowerClose || !(u is Hero)) &&
                !enemyHeroes.Any(h => h.Damage >= u.Health && 
                    Math.Min(GetOneShootAttackTime(h as Hero, u, new Point(h.X, h.Y)), GetOneShootAttackTime(h as Hero, u, GetMovingPoint(h as Hero, u, h))) < 1))
                    .OrderBy(x => GetDistance(myHero,x)).FirstOrDefault();
        if (targetUnit2 == null) targetUnit2 = EnemyTower;
        
        return new TargetUnitData 
        {
            TargetUnit = targetUnit2
        };
        
    }
    
    static void SetAggro(Hero hero, AggroThing aggroThing)
    {
        if (GetSqrDistance(aggroThing, hero) <= AGGROUNITRANGE2)
        {
            if (aggroThing.AggroTimeLeft < AGGROUNITTIME)
            {       
                aggroThing.AggroUnit = hero;
                aggroThing.AggroTSet = T;
            }
            else if (aggroThing.AggroTSet == T && aggroThing.AggroUnit != null && GetSqrDistance(aggroThing, hero) < GetSqrDistance(aggroThing, aggroThing.AggroUnit))
            {
                aggroThing.AggroUnit = hero;   
            }
            aggroThing.AggroTimeLeft = AGGROUNITTIME;
        }      
    }    
   
    static string GetMoveAttackCommand(Hero myHero, Hero otherHero, IList<Unit> myUnits, IList<Unit> enemyUnits, int heroNumber, bool isEnemyTowerTarget, TargetUnitData targetUnitData)
    {      
        
        
        
        
       var enemyCreatures = enemyUnits.Where(u => u is Creature).ToList();
        var targetUnit = targetUnitData.TargetUnit;                
        //var movingPoint = targetUnitData.OneShootKillPoint;
        if (targetUnitData.OneShootKillPoint != null && (!isEnemyTowerTarget || targetUnit is Hero || targetUnit is Tower))
        {
            if (targetUnit is Hero)
            {
                SetAggro(myHero, EnemyTower);
                foreach (Creature creature in enemyCreatures)
                {
                    SetAggro(myHero, creature);   
                }
                if (GetOneShootAttackTime(myHero, targetUnit, myHero) < 1)
                    return "ATTACK " + targetUnit.Id + "; kill hero " + targetUnit.Id; //криво работает
            }
            var desc = TwoShootsUnitId == -1 ? "; 1 shoot " : "; 2 shoots ";
            desc += targetUnit.Team == myHero.Team ? "my " : "en ";
            desc += targetUnit.Id;
            if (targetUnit is Groot) return "ATTACK " + targetUnit.Id + desc;
            return "MOVE_ATTACK " + targetUnitData.OneShootKillPoint.X + " " + targetUnitData.OneShootKillPoint.Y + " " + targetUnit.Id + desc;
        }
        
        var y = MyTower.Y;
        if (heroNumber == 0) y+= 50;
        else y -=50;
        var towerPoint = new Point(MyTower.X, y);
       
        var towerMovingPoint = GetMovingPoint(myHero, targetUnitData.TargetUnit, towerPoint);
        var behindCreaturesPoint = GetBehindCreaturesPoint(myHero, myUnits);
        Point movingPoint = null;
        if (GetDistance(MyTower, towerMovingPoint) < GetDistance(MyTower, behindCreaturesPoint))
        {           
            movingPoint = towerMovingPoint;
        }
        else
        {            
            movingPoint = behindCreaturesPoint;    
        }
                
        
        var dist = GetDistance(myHero, movingPoint);
        var time = dist / myHero.MovementSpeed;
        var attackTime = GetAttackTime(movingPoint, targetUnit, myHero);
        
        //Console.Error.WriteLine(movingPoint.X + " " + movingPoint.Y + " " + time + " " + attackTime);
        
        if (myHero.Health < myHero.MaxHealth / 4d)
        {
            var nearestTarget = enemyUnits.OrderBy(u => GetSqrDistance(u, MyTower)).FirstOrDefault();
            return "MOVE_ATTACK " + MyTower.X + " " + MyTower.Y + " " + nearestTarget.Id + "; SOS";
        }
        
         //если в нас может  попасть башня, надо уходить
         var closerToTowerCreaturesCount = myUnits.Where(u => u is Creature && 
            GetSqrDistance(EnemyTower, u) < Math.Min(GetSqrDistance(EnemyTower, myHero), GetSqrDistance(EnemyTower, movingPoint))).Count();
       
        if (isEnemyTowerTarget || 
            closerToTowerCreaturesCount <= 2 && (GetDistance(myHero, EnemyTower) <= EnemyTower.AttackRange + 200 || GetDistance(movingPoint, EnemyTower) <= EnemyTower.AttackRange + 200))
            //enemyTowerTarget == null && GetDistance(EnemyTower, movingPoint) <= EnemyTower.AttackRange)
        {
            var towersVector = new Vector
            {
                StartPoint = new Point(EnemyTower.X, EnemyTower.Y),
                EndPoint = new Point (towerPoint.X, towerPoint.Y)
            };
            
            var vectorLength = GetDistance(towersVector.StartPoint, towersVector.EndPoint);
            var towerSafeDist = EnemyTower.AttackRange + 200d;
            towersVector.Mult(towerSafeDist / vectorLength);
            var towerSafePoint = towersVector.EndPoint;
            
            var distToTowerSafePoint = GetDistance(MyTower, towerSafePoint);
            var distToMovingPoint = GetDistance(MyTower, movingPoint);
            
            var resPoint = distToTowerSafePoint < distToMovingPoint ? towerSafePoint : movingPoint;
            
            
            var nearestTarget = enemyUnits.OrderBy(u => GetSqrDistance(u, resPoint)).FirstOrDefault();
            
            //TODO: умный уход со стрельбой
            return
                "MOVE_ATTACK " + resPoint.X + " " + resPoint.Y + " " + nearestTarget.Id + "; is tower target";
        }
        else
        {     
            var nearestMeleeEnemyHero = enemyUnits.Where(u => u is Hero && !u.IsRanged).OrderBy(h => GetDistance(myHero, h)).FirstOrDefault();
            if (nearestMeleeEnemyHero != null)
            {
                var safePoint = GetSafePoint(myHero, nearestMeleeEnemyHero);
                
                if (safePoint != null) 
                {
                    var nearestTarget = enemyUnits.OrderBy(u => GetDistance(u, safePoint)).FirstOrDefault();
                    var moveAttackUnit = nearestTarget != null && GetDistance(nearestTarget, safePoint) <= myHero.AttackRange ? nearestTarget : nearestMeleeEnemyHero; 
                    if (moveAttackUnit is Hero) 
                    {
                        SetAggro(myHero, EnemyTower);
                        foreach (Creature creature in enemyCreatures)
                        {
                            SetAggro(myHero, creature);   
                        }   
                    }
                    return "MOVE_ATTACK " + safePoint.X + " " + safePoint.Y + " " + moveAttackUnit.Id + "; run from melee hero";
                }
            }
                        
            
            var hasFarCreatures = myUnits.Any(u => u is Creature && GetDistance(MyTower, u) > GetDistance(MyTower, targetUnit));
            var isNotTarget = !Targets.ContainsKey(myHero.Id) || !Targets[myHero.Id].Any(t => t != targetUnit.Id);
            if (targetUnit is Hero && targetUnit.IsRanged && hasFarCreatures && isNotTarget)
            {
                SetAggro(myHero, EnemyTower);
                foreach (Creature creature in enemyCreatures)
                {
                    SetAggro(myHero, creature);   
                }     
                return "ATTACK " + targetUnit.Id + "; attack caught hero" ;
            }
            
            if (time + attackTime > 1)
            {               
                var distToEnemy = GetDistance(myHero, targetUnit);
                if (distToEnemy > myHero.AttackRange)       
                {                    
                    var enemyHeroes = enemyUnits.Where(u => u is Hero).ToList();
                    
                   
                    
                    if (GetSqrDistance(MyTower, movingPoint) > GetSqrDistance(MyTower, myHero) &&  enemyHeroes.Any(h => 
                        Math.Min(GetOneShootAttackTime(h, myHero, new Point(h.X, h.Y)), GetOneShootAttackTime(h, myHero, GetMovingPoint(h, myHero, h))) < 1))
                    {
                         var nearestTarget = enemyUnits.OrderBy(u => GetSqrDistance(u, myHero)).FirstOrDefault();
                         return "MOVE_ATTACK " + myHero.X + " " + myHero.Y + " " + nearestTarget.Id + "; just stay" ;
                    }
                    
                    return "MOVE_ATTACK " + movingPoint.X + " " + movingPoint.Y + " " + targetUnit.Id + "; just move" ;
                }
                else
                {
                    
                    var vector = new Vector
                    {
                        StartPoint = new Point(myHero.X, myHero.Y),
                        EndPoint = new Point(movingPoint.X, movingPoint.Y)              
                    };
                    
                    var distToMovingPoint = GetDistance(vector.StartPoint, vector.EndPoint);
                    var pathLength = myHero.MovementSpeed * 0.8;
                    var coeff = pathLength * 1d / distToMovingPoint;                   
                    vector.Mult(coeff);    
                    
                    if (targetUnit is Hero)
                    {
                        SetAggro(myHero, EnemyTower);
                        foreach (Creature creature in enemyCreatures)
                        {
                            SetAggro(myHero, creature);   
                        }
                    }
                 
                    return "MOVE_ATTACK " + vector.EndPoint.X + " " + vector.EndPoint.Y + " " + targetUnit.Id + "; move behind and attack";
                }
                
            }
            else
            {          
                if (targetUnit is Hero)
                {
                    SetAggro(myHero, EnemyTower);
                    foreach (Creature creature in enemyCreatures)
                    {
                        SetAggro(myHero, creature);   
                    }              
                    if (!Targets.ContainsKey(myHero.Id) || !Targets[myHero.Id].Any(t => t != targetUnit.Id))
                    {
                         return "ATTACK " + targetUnit.Id + "; ATTACK " + targetUnit.Id;
                    }
                }
                return "MOVE_ATTACK " + movingPoint.X + " " + movingPoint.Y + " " + targetUnit.Id + "; simple att " + targetUnit.Id ;
            }
        }
    }
    
    static Point GetSafePoint(Hero hero, Unit enemyUnit)
    {
                
        var enemyHero = enemyUnit as Hero; 
        if (enemyUnit != null) 
        {
            if (!enemyHero.IsRanged)   
            {
                var moveTime = 1 - HERO_ATTACK_TIME * 2;
                var r = (int)(enemyHero.MovementSpeed * moveTime) + 1;
                
                if (GetDistance(hero, enemyHero) > r) return null;
                
                if (GetDistance(hero, MyTower) <= hero.MovementSpeed * 0.8) 
                    return new Point (MyTower.X, MyTower.Y);
                
                //var endX = 2 * enemyHero.X - hero.X;
                //var endY = 2 * enemyHero.Y - hero.Y;
                
                var point = GetLineCircleCrossPoint(hero.X, hero.Y, MyTower.X, MyTower.Y, enemyHero.X, enemyHero.Y, r);
                return point;
            }
        }
        
        
        throw new NotSupportedException();        
        
    }
    
    static Point GetLineCircleCrossPoint(int x1, int y1, int x2, int y2, int x0, int y0, int r)
    {
        var q = (y2-y1)*1d/(x2-x1);
        
        
        var a = 1 + Math.Pow(q,2);
        var b2 = y1 - y0 + q * x1 - x0;
        var c = Math.Pow(y1 - y0 - q * x1, 2) - r * r;
        
        var d1 = b2*b2 - a * c;
        if (d1 < 0) return null;
        
        var x1Res = (int) ((-b2 + Math.Sqrt(d1))*1d/a);
        var y1Res = (int) (q*x1Res - q * x1 + y1);       
        
        var x2Res = (int)((-b2 - Math.Sqrt(d1))*1d/a);
        var y2Res = (int)(q*x2Res - q * x1 + y1);
        
        if (x2 < x1)
        {
            x1Res--;
            x2Res--;
        }
        else
        {
            x1Res++;
            x2Res++;            
        }
        
        if (y2 < y1)
        {
            y1Res--;
            y2Res--;
        }
        else
        {
            y1Res++;
            y2Res++;            
        }
        
        var circleCenterPoint = new Point(x0,y0);
        
        
        var p1 = new Point(x1Res, y1Res);
        //if (GetDistance(circleCenterPoint, p1) > r) p1 = null;
        var p2 = new Point(x2Res, y2Res);
        //if (GetDistance(circleCenterPoint, p2) > r) p2 = null;
        
        //if (p1 == null && p2 == null) return null;
        //if (p1 == null) return p2;
        //if (p2 == null) return p1;        
        
        var endPoint = new Point(x2, y2);        
        if (GetDistance(endPoint, p1) < GetDistance(endPoint, p2)) return p1;
        return p2;       
        
    }
    
    static Point GetBehindCreaturesPoint(Hero hero, IList<Unit> myUnits)
    {
        var meleeCreatures = myUnits.Where(u => u is Creature).ToList();
        var faarestCreature = meleeCreatures.OrderBy(u => GetDistance(u, MyTower)).LastOrDefault();
        
        if (faarestCreature == null) return new Point(MyTower.X, hero.Y);
        
        return new Point(hero.Team == 0 ? faarestCreature.X - BEHIND_CREATURE_DIST : faarestCreature.X + BEHIND_CREATURE_DIST, hero.Y);
    }
    
}