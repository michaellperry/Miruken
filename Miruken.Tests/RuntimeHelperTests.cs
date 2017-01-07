﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Miruken.Infrastructure;

namespace Miruken.Tests
{
    [TestClass]
    public class RuntimeHelperTests
    {
        public class Handler
        {
            public object Arg1 { get; set; }
            public object Arg2 { get; set; }

            public void Handle()
            {
            }

            public void HandleOne(string arg)
            {
                Arg1 = arg;
            }

            public void HandleTwo(bool arg1, DateTime arg2)
            {
                Arg1 = arg1;
                Arg2 = arg2;
            }
        }

        public class Provider
        {
            public string Provide()
            {
                return "Hello";
            }

            public string ProvideOne(int arg)
            {
                return arg.ToString();
            }

            public DateTime ProvideTwo(int arg1, DateTime arg2)
            {
                return arg2.AddDays(arg1);
            }

            public void ProvideVoid()
            {
            }
        }

        public interface IFoo {}
        public interface IBar {}
        public interface IBoo { }
        public interface IBaz : IFoo, IBar {}
        public class Bar : IBar { }
        public class Baz : IBaz, IBoo {}

        [TestMethod]
        public void Should_Get_Toplevel_Interfaces()
        {
            var toplevel = RuntimeHelper.GetToplevelInterfaces(typeof(Bar));
            CollectionAssert.AreEqual(toplevel, new [] { typeof(IBar) });
            toplevel = RuntimeHelper.GetToplevelInterfaces(typeof(Baz));
            CollectionAssert.AreEqual(toplevel, new[] { typeof(IBaz), typeof(IBoo) });
        }

        [TestMethod]
        public void Should_Create_Single_Arg_Action()
        {
            var call = RuntimeHelper.CreateActionOneArg(
                typeof(Handler).GetMethod("HandleOne"));
            var handler = new Handler();
            call(handler, "Hello");
            Assert.AreEqual("Hello", handler.Arg1);
        }

        [TestMethod]
        public void Should_Create_Double_Arg_Action()
        {
            var call = RuntimeHelper.CreateActionTwoArgs(
                typeof(Handler).GetMethod("HandleTwo"));
            var handler = new Handler();
            call(handler, false, new DateTime(2007, 6, 14));
            Assert.AreEqual(false, handler.Arg1);
            Assert.AreEqual(new DateTime(2007, 6, 14), handler.Arg2);
        }

        [TestMethod, ExpectedException(typeof(ArgumentException),
             "Method HandlerTwo expects 2 argument(s)")]
        public void Should_Fail_If_Action_Arg_Mismatch()
        {
            RuntimeHelper.CreateFuncOneArg(
                typeof(Handler).GetMethod("HandleTwo"));
        }

        [TestMethod, ExpectedException(typeof(ArgumentException),
            "Method Handle expects 0 arguments")]
        public void Should_Fail_If_No_Args()
        {
            RuntimeHelper.CreateActionOneArg(
                typeof(Handler).GetMethod("Handle"));
        }

        [TestMethod]
        public void Should_Create_No_Arg_Function()
        {
            var call = RuntimeHelper.CreateFuncNoArgs(
                typeof(Provider).GetMethod("Provide"));
            var provider = new Provider();
            Assert.AreEqual("Hello", call(provider));
        }

        [TestMethod]
        public void Should_Create_One_Arg_Function()
        {
            var call = RuntimeHelper.CreateFuncOneArg(
                typeof(Provider).GetMethod("ProvideOne"));
            var provider = new Provider();
            Assert.AreEqual("22", call(provider, 22));
        }

        [TestMethod]
        public void Should_Create_Two_Args_Function()
        {
            var call = RuntimeHelper.CreateFuncTwoArgs(
                typeof(Provider).GetMethod("ProvideTwo"));
            var provider = new Provider();
            Assert.AreEqual(new DateTime(2003, 4, 9),
                call(provider, 2, new DateTime(2003, 4, 7)));
        }

        [TestMethod, ExpectedException(typeof(ArgumentException),
            "Method ProvideOne expects 1 argument(s)")]
        public void Should_Fail_If_Function_Arg_Mismatch()
        {
            RuntimeHelper.CreateFuncTwoArgs(
                typeof(Provider).GetMethod("ProvideOne"));
        }

        [TestMethod,ExpectedException(typeof(ArgumentException),
            "Method Provide is void")]
        public void Should_Fail_If_No_ReturnType()
        {
            RuntimeHelper.CreateActionOneArg(
                typeof(Provider).GetMethod("ProvideVoid"));
        }
    }
}