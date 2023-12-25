using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoStreamDeck.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace AutoStreamDeck.Tests
{

	[TestClass]
	public class ReflectionTests
	{

		[TestMethod]
		public void TestActionLoading()
		{
			ContextualAction contextualAction = new ContextualAction("UNIQUE_ID", "com.powerfulbacon.tests.testAction");
			contextualAction.HandleEvent("keyDown", JsonNode.Parse(@"{""coordinates"":{""column"":2,""row"":0},""isInMultiAction"":false,""settings"":{}}"));
		}

		[TestMethod]
		public void TestSomething()
		{
			Assert.Fail();
		}

	}
}
