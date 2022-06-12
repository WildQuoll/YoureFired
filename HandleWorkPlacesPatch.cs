using CitiesHarmony.API;
using ColossalFramework;
using HarmonyLib;
using System;
using UnityEngine;

namespace YoureFired
{
    [HarmonyPatch(typeof(CommonBuildingAI), "HandleWorkPlaces")]
    class HandleWorkPlacesPatch
    {
        private static void SackWorkers(ushort buildingId, int lvl0ToSack, int lvl1ToSack, int lvl2ToSack, int lvl3ToSack)
        {
            int totalToSack = lvl0ToSack + lvl1ToSack + lvl2ToSack + lvl3ToSack;

            CitizenManager citizenManager = Singleton<CitizenManager>.instance;
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            Building building = buildingManager.m_buildings.m_buffer[buildingId];

            var citizenUnits = building.m_citizenUnits;
            while (citizenUnits != 0)
            {
                uint nextUnit = citizenManager.m_units.m_buffer[citizenUnits].m_nextUnit;
                if ((citizenManager.m_units.m_buffer[citizenUnits].m_flags & CitizenUnit.Flags.Work) != 0)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        uint citizenId = citizenManager.m_units.m_buffer[citizenUnits].GetCitizen(j);
                        if (citizenId != 0)
                        {
                            Citizen citizen = citizenManager.m_citizens.m_buffer[citizenId];
                            switch (citizen.EducationLevel)
                            {
                                case Citizen.Education.Uneducated:
                                    if (lvl0ToSack > 0)
                                    {
                                        //Debug.Log(Mod.Identifier + "Building " + buildingId.ToString() + " sacking citizen " + citizenId);
                                        citizen.SetWorkplace(citizenId, 0, 0);
                                        lvl0ToSack -= 1;
                                        totalToSack -= 1;

                                        if (totalToSack == 0)
                                        {
                                            return;
                                        }
                                    }
                                    break;
                                case Citizen.Education.OneSchool:
                                    if (lvl1ToSack > 0)
                                    {
                                        //Debug.Log(Mod.Identifier + "Building " + buildingId.ToString() + " sacking citizen " + citizenId);
                                        citizen.SetWorkplace(citizenId, 0, 0);
                                        lvl1ToSack -= 1;
                                        totalToSack -= 1;

                                        if (totalToSack == 0)
                                        {
                                            return;
                                        }
                                    }
                                    break;

                                case Citizen.Education.TwoSchools:
                                    if (lvl2ToSack > 0)
                                    {
                                        //Debug.Log(Mod.Identifier + "Building " + buildingId.ToString() + " sacking citizen " + citizenId);
                                        citizen.SetWorkplace(citizenId, 0, 0);
                                        lvl2ToSack -= 1;
                                        totalToSack -= 1;

                                        if (totalToSack == 0)
                                        {
                                            return;
                                        }
                                    }
                                    break;

                                case Citizen.Education.ThreeSchools:
                                    if (lvl3ToSack > 0)
                                    {
                                        //Debug.Log(Mod.Identifier + "Building " + buildingId.ToString() + " sacking citizen " + citizenId);
                                        citizen.SetWorkplace(citizenId, 0, 0);
                                        lvl3ToSack -= 1;
                                        totalToSack -= 1;

                                        if (totalToSack == 0)
                                        {
                                            return;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }

                citizenUnits = nextUnit;
            }
        }

        private static String ToString(int[] arrayOf4)
        {
            return arrayOf4[0] + "/" + arrayOf4[1] + "/" + arrayOf4[2] + "/" + arrayOf4[3];
        }

        [HarmonyPostfix]
        public static void Postfix(ushort buildingID, ref Building data, int workPlaces0, int workPlaces1, int workPlaces2, int workPlaces3, ref Citizen.BehaviourData behaviour, int aliveWorkerCount, int totalWorkerCount)
        {
            // The vanilla behaviour appears to be as follows:
            //
            // If empty workplaces available:
            //    1/5 chance of posting job offer(s) for employees at the desired skill level(s) - priority determined by how understaffed/undereducated workforce is
            //    1/5 chance of posting job offer(s) for employees above the desired skill level(s) - lowest priority
            //    3/5 chance of nothing (until the next time this method is called - whenever that happens)
            // If all workplaces are filled, but some workers are undereducated - nothing happens (!), the building is stuck with them
            // and (if it's a private business and workforce severely undereducated) will eventually fail.

            if (data.m_citizenUnits == 0) // this seems to mean the building doesn't interact with citizens (e.g. water pump), so skip (skipped in the main function also)
            {
                return;
            }

            if (workPlaces0 >= behaviour.m_educated0Count &&
                workPlaces1 >= behaviour.m_educated1Count &&
                workPlaces2 >= behaviour.m_educated2Count &&
                workPlaces3 >= behaviour.m_educated3Count)
            {
                // Building not over-staffed at any education level
                return;
            }

            int[] numEmployeesByLevel = new int[4];
            numEmployeesByLevel[0] = behaviour.m_educated0Count;
            numEmployeesByLevel[1] = behaviour.m_educated1Count;
            numEmployeesByLevel[2] = behaviour.m_educated2Count;
            numEmployeesByLevel[3] = behaviour.m_educated3Count;

            int[] numWorkplacesByLevel = new int[4];
            numWorkplacesByLevel[0] = workPlaces0;
            numWorkplacesByLevel[1] = workPlaces1;
            numWorkplacesByLevel[2] = workPlaces2;
            numWorkplacesByLevel[3] = workPlaces3;

            //Debug.Log(Mod.Identifier + "Building " + buildingID.ToString() + " has " + ToString(numWorkplacesByLevel) + " workplaces");
            //Debug.Log(Mod.Identifier + "Building " + buildingID.ToString() + " has " + ToString(numEmployeesByLevel) + " employees");

            int[] numEmployeesByEffectiveLevel = new int[4];
            for (int i = 0; i < 4; ++i)
            {
                numEmployeesByEffectiveLevel[i] = numEmployeesByLevel[i];
            }
            for (int i = 3; i > 0; --i)
            {
                int surplus = Math.Max(0, numEmployeesByEffectiveLevel[i] - numWorkplacesByLevel[i]);
                numEmployeesByEffectiveLevel[i] -= surplus;
                numEmployeesByEffectiveLevel[i - 1] += surplus;
            }

            //Debug.Log(Mod.Identifier + "Building " + buildingID.ToString() + " has " + ToString(numEmployeesByEffectiveLevel) + " effective employees");

            int[] employeesToSackByLevel = new int[4];
            for (int i = 0; i < 4; ++i)
            {
                employeesToSackByLevel[i] = Math.Max(0, numEmployeesByEffectiveLevel[i] - numWorkplacesByLevel[i]);
            }

            //Debug.Log(Mod.Identifier + "Building " + buildingID.ToString() + " will sack " + ToString(employeesToSackByLevel) + " effective employees");

            for (int i = 0; i < 3; ++i)
            {
                int candidatesForSacking = Math.Max(0, numEmployeesByLevel[i] - numWorkplacesByLevel[i]);
                int deficit = Math.Max(0, employeesToSackByLevel[i] - candidatesForSacking);
                employeesToSackByLevel[i] -= deficit;
                employeesToSackByLevel[i + 1] += deficit;
            }

            //Debug.Log(Mod.Identifier + "Building " + buildingID.ToString() + " will sack " + ToString(employeesToSackByLevel) + " employees");

            SackWorkers(buildingID, employeesToSackByLevel[0], employeesToSackByLevel[1], employeesToSackByLevel[2], employeesToSackByLevel[3]);
        }
    }
}