using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhoneSkill.Common;
using PhoneSkill.Models;

namespace PhoneSkillTest.Common
{
    [TestClass]
    public class ContactDeduplicatorTests
    {
        [TestMethod]
        public void Test_DeduplicateByPhoneNumbers()
        {
            var contacts = new List<ContactCandidate>();

            var andrew = new ContactCandidate();
            andrew.Name = "Andrew";
            var andrewHome = new PhoneNumber();
            andrewHome.Number = "555 111 1111";
            andrewHome.Type.Standardized = PhoneNumberType.StandardType.HOME;
            andrew.PhoneNumbers.Add(andrewHome);
            var andrewBusiness = new PhoneNumber();
            andrewBusiness.Number = "555 222 2222";
            andrewBusiness.Type.Standardized = PhoneNumberType.StandardType.BUSINESS;
            andrew.PhoneNumbers.Add(andrewBusiness);
            var andrewMobile = new PhoneNumber();
            andrewMobile.Number = "555 333 3333";
            andrewMobile.Type.Standardized = PhoneNumberType.StandardType.MOBILE;
            andrew.PhoneNumbers.Add(andrewMobile);
            contacts.Add(andrew);

            var bob = new ContactCandidate();
            bob.Name = "Bob";
            var bobHome = new PhoneNumber();
            bobHome.Number = "555 444 4444";
            bobHome.Type.Standardized = PhoneNumberType.StandardType.HOME;
            bob.PhoneNumbers.Add(bobHome);
            contacts.Add(bob);

            var chris = new ContactCandidate();
            chris.Name = "Chris";
            var chrisMobile = new PhoneNumber();
            chrisMobile.Number = "555 222 2222";
            chrisMobile.Type.Standardized = PhoneNumberType.StandardType.MOBILE;
            chris.PhoneNumbers.Add(chrisMobile);
            var chrisHome = new PhoneNumber();
            chrisHome.Number = "555 555 5555";
            chrisHome.Type.Standardized = PhoneNumberType.StandardType.HOME;
            chris.PhoneNumbers.Add(chrisHome);
            contacts.Add(chris);

            var delilah = new ContactCandidate();
            delilah.Name = "Delilah";
            var delilahHome = new PhoneNumber();
            delilahHome.Number = "555 555 5555";
            delilahHome.Type.Standardized = PhoneNumberType.StandardType.HOME;
            delilah.PhoneNumbers.Add(delilahHome);
            var delilahBusiness = new PhoneNumber();
            delilahBusiness.Number = "555 666 6666";
            delilahBusiness.Type.Standardized = PhoneNumberType.StandardType.BUSINESS;
            delilah.PhoneNumbers.Add(delilahBusiness);
            contacts.Add(delilah);

            var dedupedContacts = ContactDeduplicator.DeduplicateByPhoneNumbers(contacts);
            CheckThatPhoneNumbersAreUnique(dedupedContacts);

            var expectedContacts = new List<ContactCandidate>();

            var expectedAndrew = new ContactCandidate();
            expectedAndrew.Name = "Andrew";
            var expectedAndrewHome = new PhoneNumber();
            expectedAndrewHome.Number = "555 111 1111";
            expectedAndrewHome.Type.Standardized = PhoneNumberType.StandardType.HOME;
            expectedAndrew.PhoneNumbers.Add(expectedAndrewHome);
            var expectedAndrewBusiness = new PhoneNumber();
            expectedAndrewBusiness.Number = "555 222 2222";
            expectedAndrewBusiness.Type.Standardized = PhoneNumberType.StandardType.BUSINESS;
            expectedAndrew.PhoneNumbers.Add(expectedAndrewBusiness);
            var expectedAndrewMobile = new PhoneNumber();
            expectedAndrewMobile.Number = "555 333 3333";
            expectedAndrewMobile.Type.Standardized = PhoneNumberType.StandardType.MOBILE;
            expectedAndrew.PhoneNumbers.Add(expectedAndrewMobile);
            var expectedChrisHome = new PhoneNumber();
            expectedChrisHome.Number = "555 555 5555";
            expectedChrisHome.Type.Standardized = PhoneNumberType.StandardType.HOME;
            expectedAndrew.PhoneNumbers.Add(expectedChrisHome);
            var expectedDelilahBusiness = new PhoneNumber();
            expectedDelilahBusiness.Number = "555 666 6666";
            expectedDelilahBusiness.Type.Standardized = PhoneNumberType.StandardType.BUSINESS;
            expectedAndrew.PhoneNumbers.Add(expectedDelilahBusiness);
            expectedContacts.Add(expectedAndrew);

            var expectedBob = new ContactCandidate();
            expectedBob.Name = "Bob";
            var expectedBobHome = new PhoneNumber();
            expectedBobHome.Number = "555 444 4444";
            expectedBobHome.Type.Standardized = PhoneNumberType.StandardType.HOME;
            expectedBob.PhoneNumbers.Add(expectedBobHome);
            expectedContacts.Add(expectedBob);

            CheckContacts(expectedContacts, dedupedContacts);
        }

        private void CheckThatPhoneNumbersAreUnique(IList<ContactCandidate> contacts)
        {
            var seenPhoneNumbers = new Dictionary<string, ContactCandidate>();
            foreach (var contact in contacts)
            {
                foreach (var phoneNumber in contact.PhoneNumbers)
                {
                    if (!seenPhoneNumbers.TryAdd(phoneNumber.Number, contact))
                    {
                        var otherContact = seenPhoneNumbers[phoneNumber.Number];
                        Assert.Fail($"Duplicate phone number \"{phoneNumber.Number}\" between {contact} and {otherContact}");
                    }
                }
            }
        }

        private void CheckContacts(IList<ContactCandidate> expectedContacts, IList<ContactCandidate> contacts)
        {
            CollectionAssert.AreEqual((ICollection)expectedContacts, (ICollection)contacts, $"Expected: {expectedContacts.ToPrettyString()}\nActual: {contacts.ToPrettyString()}\n");
        }
    }
}
