﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BE;
using DS;
using DAL;
using GoogleMapsApi.Entities.Directions.Request;
using GoogleMapsApi.Entities.Directions.Response;
using GoogleMapsApi;

namespace BL
{
    class IBL_imp : IBL
    {
        Idal mydal = FactoryDal.getDal();
        #region Nanny functions
        public void addNanny(Nanny n)
        {
            if (n.BirthDate.Year < 1900 || ((DateTime.Now.Year - n.BirthDate.Year) < 18))
                throw new Exception("not aged enough");
            mydal.addNanny(n);
        }
        public void removeNanny(Nanny n) => mydal.removeNanny(n);
        public void updateNanny(Nanny n) => mydal.updateNanny(n);
        public Nanny getNanny(long id) => mydal.getNanny(id);
        public List<Nanny> getNannyList() => mydal.getNannyList();
        #endregion
        #region Mother functions
        public void addMother(Mother m) => mydal.addMother(m);
        public void removeMother(Mother m) => mydal.removeMother(m);
        public void updateMother(Mother m) => mydal.updateMother(m);
        public Mother getMother(long id) => mydal.getMother(id);
        public List<Mother> getMotherList() => mydal.getMotherList();
        #endregion
        #region Child functions
        public void addChild(Child c) => mydal.addChild(c);
        public void removeChild(Child c) => mydal.removeChild(c);
        public void updateChild(Child c) => mydal.updateChild(c);
        public Child getChild(long id) => mydal.getChild(id);
        public List<Child> getChildList(List<Mother> m) => mydal.getChildList(m);
        #endregion
        #region BankAccount functions
        public List<BankAccount> getBanksAccountList()
        {
            throw new NotImplementedException();
        }
        public List<string> getBanksNameList(List<BankAccount> a)
        {
            throw new NotImplementedException();
        }
        public List<int> getBanksBrancheList(List<BankAccount> a)
        {
            throw new NotImplementedException();
        }
        #endregion
        #region Contract functions
        //discount based on number of children of the mother 
        public void salary(Contract c)
        {
            float hourly = getNanny(c.ID_nanny).HourlyRate;
            float monthly = getNanny(c.ID_nanny).MonthlyRate;
            int numOfChildren = getMother(c.ID_mother).myChildren.Count();
            c.Wages_per_hours = (hourly / 100) * (100 - numOfChildren * 2);
            c.Wages_per_months = (monthly / 100) * (100 - numOfChildren * 2);
        }
        public void addContract(Contract c) => addContract(c, mydal.getMother(c.ID_mother));
        //verifing ages, nanny avalability, adding all factors to nanny and the contract
        public void addContract(Contract c, Mother m)
        {
            try
            {
                check_mother_and_nanny(c);
                check_child_age(m);
                if (getNanny(c.ID_nanny).numOfChildren >= getNanny(c.ID_nanny).MaxNumOfChildren)
                    throw new Exception("Nanny have reached the maximum number of children alowed");
                getNanny(c.ID_nanny).numOfChildren += 1;
                salary(c);
                dist(c);
                setPayment(c);
                mydal.addContract(c);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
        }
        //return the distance between mother and nanny
        public void dist(Contract c)
        {
            string ad = mydal.getMother(c.ID_mother).Address.ToAddress();
            string nan = mydal.getNanny(c.ID_nanny).Address.ToAddress();
            var drivingDirectionRequest = new DirectionsRequest
            {
                TravelMode = TravelMode.Walking,
                Origin = ad,
                Destination = nan
            };
            DirectionsResponse drivingDirections = GoogleMaps.Directions.Query(drivingDirectionRequest);
            Route route = drivingDirections.Routes.First();
            Leg leg = route.Legs.First();
            c.distance = leg.Distance.Value/1000;
            Console.WriteLine(leg.Distance.Value+"------------------");
        }
        //checking child's age (has to be older then 3 months)
        public void check_child_age(Mother m)
        {
            var v = m.myChildren;
            if (v.Count() == 0)
                throw new Exception("the children is not existing in the system");
            foreach (Child ch in v)
                if (ch.Age < 3) throw new Exception("the child is too young");
        }
        //checking mother and nanny existance
        public void check_mother_and_nanny(Contract c)
        {
            var v = mydal.getMotherList().Where(t => t.ID == c.ID_mother);
            if (v.Count() == 0)
                throw new Exception("the mother is not existing in the system");
            var z = mydal.getNannyList().Where(t => t.ID == c.ID_nanny);
            if (z.Count() == 0)
                throw new Exception("the nanny id not existing in the system");
        }
        public void removeContract(Contract c) => mydal.removeContract(c);
        public void updateContract(Contract c) => mydal.updateContract(c);
        public List<Contract> getContractList() => DataSource.contracts;
        #endregion
        //finding all nannies within mother's given range
        public List<Nanny> Nanny_In_Range(Mother m)
        {
            List<Nanny> ans = new List<Nanny>();
            string ad = m.Address.ToAddress();
            Console.WriteLine(ad);
            foreach (Nanny n in DataSource.Nannies)
            {
                string nan = n.Address.ToAddress();
                Console.WriteLine(nan);
                var drivingDirectionRequest = new DirectionsRequest
                {
                    TravelMode = TravelMode.Walking,
                    Origin = ad,
                    Destination = nan
                };
                DirectionsResponse drivingDirections = GoogleMaps.Directions.Query(drivingDirectionRequest);
                Route route = drivingDirections.Routes.First();
                Leg leg = route.Legs.First();
                if (leg.Distance.Value/1000 <= m.Range) ans.Add(n);
                Console.WriteLine(leg.Distance.Value);
            }
            return ans;
        }
        public int Age(DateTime birthday)
        {
            DateTime now = DateTime.Today;
            int age = now.Year - birthday.Year;
            if (now < birthday.AddYears(age)) age--;
            return age;
        }
        //return all nannies working in wanted time
        public List<Nanny> Nanny_For_Mother(Mother m)
        {
            List<Nanny> n = new List<Nanny>();
            foreach (var item1 in DS.DataSource.Nannies)
            {
                bool flag = true;
                foreach (var d in m.WorkDays)
                {
                    if (item1.WorkDays.ContainsKey(d.Key))
                    {
                        if (!((d.Value.Key >= item1.WorkDays[d.Key].Key) && (d.Value.Value <= item1.WorkDays[d.Key].Value)))
                        {
                            flag = false;
                            break;
                        }
                    }
                    else
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                    n.Add(item1);
            }
            return n;
        }
        //return all children which doen't has ananny
        public List<Child> Children_without_Nanny(List<Child> c)
        {
            List<Child> noNanny = new List<Child>();
            foreach (var item1 in c)
                if (!(DS.DataSource.contracts.Any(contract => contract.ID_child == item1.ID_child)))
                    noNanny.Add(item1);
            return noNanny;
        }
        //nannies who works and has vocations by low 
        public List<Nanny> Vocations_by_Ministry_of_Economy_and_Industry(List<Nanny> n)
        {
            List<Nanny> vocations = new List<Nanny>();
            foreach (var item in n)
                if (item.Ministry_Vocations)
                    vocations.Add(item);
            return vocations;
        }
        public IEnumerable<Contract> all_contract_by_condition(Predicate<Contract> function = null)
        {
            IEnumerable<Contract> a = mydal.getContractList().Where(t => (function(t)));
            return a;
        }
        //count of contracts which follows certain condition
        public int num_of_contract_by_condition(Func<Contract, bool> function = null)
        {
            IEnumerable<Contract> a = mydal.getContractList().Where(t => (function(t)));
            return a.Count();
        }
        public IEnumerable<IGrouping<float, Nanny>> Nannies_by_Children_Ages(bool b = false)
        {
            var temp = from t in DS.DataSource.contracts
                       group mydal.getNanny(t.ID_nanny) by mydal.getChild(t.ID_child).Age;
            if (b)
                temp.OrderBy(c => c.Key);
            return temp;
        }
        public IEnumerable<IGrouping<KeyValuePair<string,string>, Nanny>> Nannies_by_address(bool b = false)
        {
            var kuku = from nan in mydal.getNannyList()
                       group nan by new KeyValuePair<string,string>(nan.Address.City, nan.Address.Street ) into c
                       select c;
            return kuku;
        }
        public IEnumerable<IGrouping<float, Nanny>> Ages_of_Children_with_Nanny(bool b = false)
        {
            var temp = mydal.getNannyList().GroupBy(a => a.MinAgeOfChild);
            if (b)
                temp.OrderBy(c => c.Key);
            return temp;
        }
        public IEnumerable<IGrouping<float, Contract>> Distance_Nanny_and_Child(bool b = false)
        {
            var temp = mydal.getContractList().GroupBy(a => a.distance);
            if (b)
            {
                temp.OrderBy(c => c.Key);
            }
            return temp;
        }
        public void setPayment(Contract c)
        {
            if (mydal.getMother(c.ID_mother).payment) c.payment = c.Wages_per_months;
            else c.payment = c.Wages_per_hours * c.hours_Of_Employment;
        }
        public void monthlyPayment(Contract c)
        {
            if (c.payment == 0) setPayment(c);
            try
            {
                mydal.getMother(c.ID_mother).BankAccount.add(-c.payment);
                mydal.getNanny(c.ID_nanny).BankAccount.add(c.payment);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
