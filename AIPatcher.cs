using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Fenix_NoAI_ESP
{

    //After digging through the code and following the execution paths of various methods related to keywords of EnemyLook etc.
    //and examining the AI config option ENEMY_LOOK_AT_ME_ANG and where it gets used I found two methods that were suspect. IsEnemyLookingAtMe and IsEnemyLookAtMeForPeriod.
    //Ultimately I change only IsEnemyLookingAtMe because IsEnemyLookAtMeForPeriod references variables that get written to by calling the IsEnemyLookingAtMe method anyway.
    //IsEnemyLookAtMeForPeriod(float period) gets called during the EndHoldPosition() method check. It references some
    //float variables and checks them, specifically is float_4 > period then return true. Then the AI appears to break cover and engage
    //based on this code snippet it is hard-coded to a period of 2f which I assume is seconds:

    //if (this.botOwner_0.EnemyLookData.IsEnemyLookAtMeForPeriod(2f))
    //{
    //	this.gclass290_0.AllFollowersDoAttack();
    //	return new GStruct7("LOOKTOSECTA", true);
    //  }

    //Now I found float_4 gets written to during the DoCheck() method. Which checks if the goalEnemy != null && goalEnemy.Distance < 20f && !this.bool_0 || goalEnemy.IsVisible. If so it runs the 
    //IsEnemyLookingAtMe(GClass442 goalEnemy) method, which calls the IsEnemyLookingAtMe(IAIDetails gamePerson) method.
    //In here it runs some GClass733.IsAngLessNormalized() method which takes arguments from the AI's weaponRoot.position, the gamePerson weaponRoot.position, and the gamePerson.LookDirection and checks if
    //they are greater than a cos variable hard coded to 0.9659258f

    //public bool IsEnemyLookingAtMe(IAIDetails gamePerson)
    //{
    //    Vector3 arg_18_0 = this.WeaponRoot.position;
    //    BifacialTransform weaponRoot = gamePerson.WeaponRoot;
    //    return GClass733.IsAngLessNormalized(GClass733.NormalizeFastSelf(arg_18_0 - weaponRoot.position), gamePerson.LookDirection, 0.9659258f);
    //}

    //public static bool IsAngLessNormalized(Vector3 a, Vector3 b, float cos)
    //{
    //    return a.x * b.x + a.y * b.y + a.z * b.z > cos;
    //}

    //As far as I can tell this SHOULD be checking if the AI is being looked at by someone facing them? Maybe? I'm honestly not sure what it is supposed to do. 
    //In my experience an AI hundreds of meters away facing directly away from me examining some crate in some holdposition loop will not notice me until I put my crosshair over them. 
    //I crawled through grass to sneak up on them, finally had the shot, and as soon as I put my crosshair over the back of their head they spun around terminator style and started shooting at me getting hits.
    //I hate how immersion breaking this is as stealth is no longer fun... 
    //So what is this IsEnemyLookingAtMe method used for? I'm not versed enough to know how it should work and fix it. Maybe get the angle right so they have to be facing you and be close enough to see you, even then if you're hiding
    //in grass you wouldn't want them to notice you just because you looked at them. So lets see if I can bypass it entirely:
    //In GClass267 there are a series of nested methods. ShallAttack() does two bool checks. One is method_2(boss, targetPlayer) OR if some series of other settings for warning a player and the time to attack are met. 
    //method_2 calls method_3, and method_3 checks if method_4 returns true and then if so checks some variables like GClass527.Core.LOOK_TIMES_TO_KILL_PLAYER or LOOK_TIMES_TO_KILL_BOT which I assume
    //are how long they have to be looked at or how many times they have to be looked at (it was hard to follow) before returning true. But that only gets checked if method_4 returns true, and I don't think it should matter
    //how long someone is looking at you for if you DON'T KNOW THEY ARE THERE.
    //So method_4 checks a few things: is the player HandsController.IsAiming true AND IsEnemyLookingAtMe(player) also true? Then return true. This is the key to why aiming at them causes them to go terminator mode.
    //When all of these return true and end up back in ShallAttack the player gets added to a list of targets in the bots memory. 
    //Now I also saw this used in GClass272 method_1(), where it checks various things like CHECK_COVER_ENEMY_LOOK variable, is the enemy visible, what is my ENEMY_DIST_TO_GO_OUT of cover variable etc.
    //And finally IsEnemyLookingAtMe. If that returns true it checks the DIST_CHECK_SFETY setting and then does a raycast from the player to the bot. If that returns true it sets Spotted(false, null, null) and calls UseDogFightOut().
    //Which as far as I can tell engages the bot in attack mode. As in are they looking at me AND their weapon muzzle is facing me, then go attack mode. 
    //I examined the Spotted() method and the first flag is for if it is from a hit or not. If it is from a hit and they know who shot them it puts their exact position in. Otherwise it adds a DangerPoint to investigate where the shot came from.
    //So if we remove this "lookatme" terminator stuff they should still react to being shot, and there are routines to handle shots landing NEAR them too. There are routines to handle if they spot you, and in my testing AI have still noticed
    //me first and blown me away before I can react. These are the only places I could find the LookAtMe check being done so I think it's perfectly safe to override it.
    //However I chose to only do so if the target looking at the bot is a player, because I didn't want to affect the AI. I suspect firefights on big maps like woods/customs etc. would be less often if I remove this
    //as bots wouldn't begin engaging each other unless they were close enough. This could be offset by tweaking the view distance settings using Fin's AI Tweaks or AI Loader etc. But I haven't experimented with that yet.


    //private void method_1()
    //{
    //    if (!this.botOwner_0.Settings.FileSettings.Cover.CHECK_COVER_ENEMY_LOOK)
    //    {
    //        return;
    //    }
    //    GClass442 goalEnemy = this.botOwner_0.Memory.GoalEnemy;
    //    if (goalEnemy == null || !goalEnemy.IsVisible)
    //    {
    //        return;
    //    }
    //    if (goalEnemy.Distance < this.botOwner_0.Settings.FileSettings.Cover.ENEMY_DIST_TO_GO_OUT)
    //    {
    //        this.botOwner_0.Memory.Spotted(false, null, null);
    //        this.botOwner_0.Memory.UseDogFightOut();
    //        return;
    //    }
    //    if (this.botOwner_0.IsEnemyLookingAtMe(goalEnemy))
    //    {
    //        Vector3 vector = this.botOwner_0.Transform.position - goalEnemy.CurrPosition;
    //        float magnitude = vector.get_magnitude();
    //        if (magnitude < this.botOwner_0.Settings.FileSettings.Cover.DIST_CHECK_SFETY && !Physics.Raycast(new Ray(goalEnemy.Person.WeaponRoot.position, vector), magnitude, GClass703.HighPolyWithTerrainMask))
    //        {
    //            this.botOwner_0.Memory.Spotted(false, null, null);
    //            this.botOwner_0.Memory.UseDogFightOut();
    //        }
    //    }
    //}
    //public void Spotted(bool byHit, Vector3? from = null, float? secToBeSpotted = null)
    //{
    //    if (this.IsInCover)
    //    {
    //        if (this.CurCustomCoverPoint.AlwaysGood && this.botOwner_0.Profile.Info.Settings.Role == WildSpawnType.marksman)
    //        {
    //            return;
    //        }
    //        float period = secToBeSpotted ?? this.botOwner_0.Settings.FileSettings.Cover.MAX_SPOTTED_TIME_SEC;
    //        using (List<CustomNavigationPoint>.Enumerator enumerator = this.botOwner_0.BotsGroup.CoverPointMaster.GetClosePoints(this.botOwner_0.Transform.position, this.botOwner_0, this.botOwner_0.Settings.FileSettings.Cover.SPOTTED_COVERS_RADIUS).GetEnumerator())
    //        {
    //            while (enumerator.MoveNext())
    //            {
    //                enumerator.Current.Spotted(period);
    //            }
    //        }
    //        this.IsInCover = false;
    //        this.BotCurrentCoverInfo.Spotted();
    //        if (byHit && from.HasValue && (this.GoalEnemy == null || this.GoalEnemy.Distance > this.botOwner_0.LookSensor.MaxShootDist))
    //        {
    //            this.botOwner_0.DangerPointsData.AddPointOfDanger(new GClass254(from.Value, PlaceForCheckType.simple), true);
    //        }
    //        if (byHit)
    //        {
    //            Action<Vector3?> expr_143 = this.action_3;
    //            if (expr_143 != null)
    //            {
    //                expr_143(from);
    //            }
    //        }
    //    }
    //    this.botOwner_0.StationaryWeaponData.Spotted();
    //}

    //Tell HarmonyLib (via Aki.Reflection) to get the IsEnemyLookingAtMe method from EFT.BotOwner where it is the one with a parameter of type IAIDetails
    //Otherwise it would throw an ambiguous name error because there is one with a parameter of GClass442 as well which just calls this IAIDetails one in the end anyway.
    //I don't get an instance of it or use any other binding flags, I want the method overriden entirely not just an instance of it.

    //Using a Prefix patch we can override the original method and change it's execution. 
    //I use ref bool __result so I can modify the result, because this original method it returns a bool therefore __result must be a bool.
    //I include the gamePerson parameter and do NOT ref it so we can read it but not change it.
    //I check if gamePerson.IsAI is not true (therefore the gamePerson looking at the bot currently must be a player). And if so I force the __result of execution to be false
    //as in NO they are not looking at me (even though we might be) and then return false. 
    //Returning true of false here has nothing to do with the result of executing the method.
    //It just tells Harmony whether it should continue to execute the original code, for eg if we changed an input parameter but still wanted the original to execute, or if we want to skip it. False skips it and True executes original code. 
    //This lets the method execute normally when a bot is looking at another bot and they can terminator mode engage each other. I assume this is fine because if a bot is looking at another bot it's because they were spotted already and now being aimed at.
    //I have added options to turn these on or off as desired so you can apply this to bots or not or even turn ESP back on for the player. 

    public class AIESPPatcherAimPlayer : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.BotOwner).GetMethod("IsEnemyLookingAtMe", new Type[] { typeof(IAIDetails) });
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref bool __result, IAIDetails gamePerson)
        {


            //Only skip original method execution if gamePerson is a player.
            if (!gamePerson.IsAI)
            {
                    //If gamePerson is a player and the setting is turned on, skip execution and return false as in NO I'm not being looked at.
                    __result = false;
                    return false;
            }
            //Otherwise return true and continue executing the original game code. 
            return true;
        }
    }

    public class AIESPPatcherAimBot : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.BotOwner).GetMethod("IsEnemyLookingAtMe", new Type[] { typeof(IAIDetails) });
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref bool __result, IAIDetails gamePerson)
        {


            //Only skip original method execution if gamePerson is a bot.
            if (gamePerson.IsAI)
            {
                //If gamePerson is a bot and the setting is turned on, skip execution and return false as in NO I'm not being looked at.
                __result = false;
                return false;
            }
            //Otherwise return true and continue executing the original game code. 
            return true;
        }
    }

    public class AIESPPatcherAimBoth : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.BotOwner).GetMethod("IsEnemyLookingAtMe", new Type[] { typeof(IAIDetails) });
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref bool __result, IAIDetails gamePerson)
        {
            //Always Skip the original method

            __result = false;
            return false;

        }
    }
}
