using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Collections;

namespace SnapShotStore
{
    public class Account2
    {
        public Account2(string accountID, string name, string description, int age)
        {
            State = new Hashtable
            {
                ["AccountID"] = accountID,
                ["Name"] = accountID,
                ["Description"] = accountID,
                ["Age"] = accountID
            };
        }

        public Hashtable State { get; private set; }
    }





    /*

    "AccountID" string(15)  NOT NULL ,
    "CompanyIDCustomerID" string(26)  NOT NULL ,
    "AccountTypeID" string(6)  NOT NULL ,
    "PrimaryAccountCodeID" string(6)  NOT NULL ,
    "PortfolioID" int  NOT NULL ,
    "ContractDate" date  NOT NULL ,
    "DelinquencyHistory" string(max)  NOT NULL ,
    "LastPaymentAmount" money  NOT NULL ,
    "LastPaymentDate" date  NOT NULL ,
    "SetupDate" date  NOT NULL ,
    "CouponNumber" int  NOT NULL ,
    "AlternateAccountNumber" string(20)  NOT NULL ,
    "Desc1" string(6)  NOT NULL ,
    "Desc2" string(6)  NOT NULL ,
    "Desc3" string(6)  NOT NULL ,
    "ConversionAccountID" string(30)  NOT NULL ,
    "SecurityQuestionsAnswered" bit  NOT NULL ,
    "LegalName" string(1000)  NOT NULL ,
     */
}
