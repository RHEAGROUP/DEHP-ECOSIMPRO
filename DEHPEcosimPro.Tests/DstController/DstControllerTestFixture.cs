﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstControllerTestFixture.cs" company="RHEA System S.A.">
//    Copyright (c) 2020-2020 RHEA System S.A.
// 
//    Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski.
// 
//    This file is part of DEHPEcosimPro
// 
//    The DEHPEcosimPro is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 3 of the License, or (at your option) any later version.
// 
//    The DEHPEcosimPro is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
// 
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program; if not, write to the Free Software Foundation,
//    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DEHPEcosimPro.Tests.DstController
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using DEHPCommon.HubController.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Services.OpcConnector.Interfaces;

    using Moq;

    using NUnit.Framework;

    using Opc.Ua;
    using Opc.Ua.Client;

    [TestFixture]
    public class DstControllerTestFixture
    {
        private DstController controller;
        private Mock<IOpcClientService> opcClient;
        private Mock<IHubController> hubController;
        private Mock<IOpcSessionHandler> opcSessionHandler;

        private List<ReferenceDescription> referenceDescriptionCollection = new List<ReferenceDescription>
        {
            new ReferenceDescription { NodeId = ExpandedNodeId.Parse("server_methods"), BrowseName = new QualifiedName("server_methods"), NodeClass = NodeClass.Object},
            new ReferenceDescription { NodeId = ExpandedNodeId.Parse("method_run"), BrowseName = new QualifiedName("method_run"), NodeClass = NodeClass.Method},
            new ReferenceDescription { NodeId = ExpandedNodeId.Parse("method_reset"),BrowseName = new QualifiedName("method_reset"), NodeClass = NodeClass.Method},
        };

        [SetUp]
        public void Setup()
        {
            this.hubController = new Mock<IHubController>();
            this.opcSessionHandler = new Mock<IOpcSessionHandler>();

            this.opcClient = new Mock<IOpcClientService>();
            this.opcClient.Setup(x => x.Connect(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IUserIdentity>())).Returns(Task.CompletedTask);
            this.opcClient.Setup(x => x.CloseSession());
            this.opcClient.Setup(x => x.ReadNode(Variables.Server_ServerStatus_StartTime)).Returns(new DataValue(new DateTime(2021, 1, 1)));
            this.opcClient.Setup(x => x.ReadNode(Variables.Server_ServerStatus_CurrentTime)).Returns(new DataValue(new DateTime(2021, 1, 3)));

            this.controller = new DstController(this.opcClient.Object, this.hubController.Object, this.opcSessionHandler.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.IsFalse(this.controller.IsSessionOpen);
            Assert.IsEmpty(this.controller.Variables);
            Assert.IsNull(this.controller.References);
            Assert.IsEmpty(this.controller.Methods);
            Assert.Null(this.controller.ServerAddress);
            Assert.Zero(this.controller.RefreshInterval);
        }

        [Test]
        public void VerifyConnect()
        {
            Assert.DoesNotThrowAsync(async () => await this.controller.Connect("endpoint"));
            this.opcClient.Verify(x => x.Connect(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IUserIdentity>()), Times.Once);
        }

        [Test]
        public void VerifyClose()
        {
            this.controller.CloseSession();
            this.opcClient.Verify(x => x.CloseSession(), Times.Once);
        }

        [Test]
        public void VerifyAddSubscription()
        {
            Assert.DoesNotThrow(() =>
            {
                this.controller.AddSubscription(new ReferenceDescription() { NodeId = new ExpandedNodeId(Guid.NewGuid()) });
                this.controller.AddSubscription(new ReferenceDescription() { NodeId = new ExpandedNodeId(Guid.NewGuid()) });
            });

            this.opcClient.Verify(x => x.AddSubscription(It.IsAny<NodeId>()), Times.Exactly(2));

            this.controller.ClearSubscriptions();
            this.opcSessionHandler.Verify(x => x.ClearSubscriptions(), Times.Once);
        }

        [Test]
        public void VerifyCallServerMethod()
        {
            this.opcClient.Setup(x => x.References).Returns(new ReferenceDescriptionCollection(this.referenceDescriptionCollection));

            foreach (var referenceDescription in this.referenceDescriptionCollection.Where(r => r.NodeClass == NodeClass.Method))
            {
                this.controller.Methods.Add(referenceDescription);
            }

            Assert.IsNull(this.controller.CallServerMethod("unknown_method"));

            Assert.DoesNotThrow(() => this.controller.CallServerMethod("method_run"));
            this.opcClient.Verify(x => x.CallMethod(new NodeId("server_methods"), new NodeId("method_run"), string.Empty), Times.Once);

            Assert.DoesNotThrow(() => this.controller.CallServerMethod("method_reset"));
            this.opcClient.Verify(x => x.CallMethod(new NodeId("server_methods"), new NodeId("method_reset"), string.Empty), Times.Once);
        }

        [Test]
        public void VerifyGetServerStartTime()
        {
            Assert.IsNull(this.controller.GetServerStartTime());

            this.controller.IsSessionOpen = true;
            Assert.AreEqual(new DateTime(2021, 1, 1), this.controller.GetServerStartTime());
            this.opcClient.Verify(x => x.ReadNode(Variables.Server_ServerStatus_StartTime), Times.Once);
        }

        [Test]
        public void VerifyGetCurrentServerTime()
        {
            Assert.IsNull(this.controller.GetCurrentServerTime());

            this.controller.IsSessionOpen = true;
            Assert.AreEqual(new DateTime(2021, 1, 3), this.controller.GetCurrentServerTime());
            this.opcClient.Verify(x => x.ReadNode(Variables.Server_ServerStatus_CurrentTime), Times.Once);
        }
    }
}
