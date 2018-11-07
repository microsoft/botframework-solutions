using EmailSkill;
using EmailSkillTest.API.Fakes;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EmailSkillTest.API
{
    [TestClass]
    public class StepHelperTests
    {
        private const string dialogId = "test";
        private static EmailSkillDialog emailSkillDialog;
        private static ISkillConfiguration services;
        private static MockDialogStateAccessor mockDialogStateAccessor;
        private static MockMailSkillServiceManager mockMailSkillServiceManager;
        private static MockEmailStateAccessor mockEmailStateAccessor;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            services = new MockSkillConfiguration();
            mockDialogStateAccessor = new MockDialogStateAccessor();
            mockMailSkillServiceManager = new MockMailSkillServiceManager();
            mockEmailStateAccessor = new MockEmailStateAccessor();
        }

        [TestMethod]
        public async Task GetNameListStringTest_OneOption()
        {
            IStatePropertyAccessor<DialogState> dialogStatePropertyAccessor = mockDialogStateAccessor.GetMock().Object;

            // Mock data
            mockEmailStateAccessor.MockEmailSkillState = new EmailSkillState();
            mockEmailStateAccessor.MockEmailSkillState.Recipients = GetRecipients(1);
            mockEmailStateAccessor.SetMockBehavior();
            IStatePropertyAccessor<EmailSkillState> emailStatePropertyAccessor = mockEmailStateAccessor.GetMock().Object;

            emailSkillDialog = new EmailSkillDialog(dialogId, services, emailStatePropertyAccessor, dialogStatePropertyAccessor, mockMailSkillServiceManager);
            var nameList = await emailSkillDialog.GetNameListStringAsync(null);

            Assert.AreEqual(nameList, "test0");
        }

        [TestMethod]
        public async Task GetNameListStringTest_TwoOptions()
        {
            IStatePropertyAccessor<DialogState> dialogStatePropertyAccessor = mockDialogStateAccessor.GetMock().Object;

            // Mock data
            mockEmailStateAccessor.MockEmailSkillState = new EmailSkillState();
            mockEmailStateAccessor.MockEmailSkillState.Recipients = GetRecipients(2);
            mockEmailStateAccessor.SetMockBehavior();
            IStatePropertyAccessor<EmailSkillState> emailStatePropertyAccessor = mockEmailStateAccessor.GetMock().Object;

            emailSkillDialog = new EmailSkillDialog(dialogId, services, emailStatePropertyAccessor, dialogStatePropertyAccessor, mockMailSkillServiceManager);
            var nameList = await emailSkillDialog.GetNameListStringAsync(null);

            Assert.AreEqual(nameList, "test0 and test1");
        }

        [TestMethod]
        public async Task GetNameListStringTest_ThreeOptions()
        {
            IStatePropertyAccessor<DialogState> dialogStatePropertyAccessor = mockDialogStateAccessor.GetMock().Object;

            // Mock data
            mockEmailStateAccessor.MockEmailSkillState = new EmailSkillState();
            mockEmailStateAccessor.MockEmailSkillState.Recipients = GetRecipients(3);
            mockEmailStateAccessor.SetMockBehavior();
            IStatePropertyAccessor<EmailSkillState> emailStatePropertyAccessor = mockEmailStateAccessor.GetMock().Object;

            emailSkillDialog = new EmailSkillDialog(dialogId, services, emailStatePropertyAccessor, dialogStatePropertyAccessor, mockMailSkillServiceManager);
            var nameList = await emailSkillDialog.GetNameListStringAsync(null);

            Assert.AreEqual(nameList, "test0, test1 and test2");
        }

        [TestMethod]
        public async Task FormatRecipientListTest()
        {
            var personData = GetPersonLists(0, 5);
            var contactData = GetPersonLists(1, 6);
            personData.AddRange(contactData);

            List<Person> originPersonList = personData;
            List<Person> originUserList = GetPersonLists(2, 7);

            (var personList, var userList) = EmailSkillDialog.FormatRecipientList(originPersonList, originUserList);

            Assert.AreEqual(personList.Count, 6);
            Assert.AreEqual(userList.Count, 1);
        }

        private List<Recipient> GetRecipients(int count)
        {
            List<Recipient> result = new List<Recipient>();

            for (int i = 0; i < count; ++i)
            {
                var recipient = new Recipient();
                recipient.EmailAddress = new EmailAddress();
                recipient.EmailAddress.Name = "test" + i.ToString();
                recipient.EmailAddress.Address = "test" + i.ToString() + "@test.com";

                result.Add(recipient);
            }

            return result;
        }

        private List<Person> GetPersonLists(int start, int end)
        {
            List<Person> result = new List<Person>();

            for (int i = start; i < end; ++i)
            {
                var emailList = new List<ScoredEmailAddress>();
                var scoredEmailAddress = new ScoredEmailAddress();
                scoredEmailAddress.Address = "test" + i.ToString() + "@test.com";
                emailList.Add(scoredEmailAddress);

                var person = new Person();
                person.DisplayName = "test" + i.ToString();
                person.ScoredEmailAddresses = emailList;

                result.Add(person);
            }

            return result;
        }
    }
}
