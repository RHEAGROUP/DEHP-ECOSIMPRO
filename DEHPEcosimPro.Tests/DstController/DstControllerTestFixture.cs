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
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;

    using CDP4Dal;
    using CDP4Dal.Operations;

    using DEHPCommon.Enumerators;
    using DEHPCommon.HubController.Interfaces;
    using DEHPCommon.MappingEngine;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using DEHPEcosimPro.DstController;
    using DEHPEcosimPro.Enumerator;
    using DEHPEcosimPro.Services.OpcConnector.Interfaces;
    using DEHPEcosimPro.ViewModel.Rows;

    using Moq;

    using NUnit.Framework;

    using Opc.Ua;

    using Node = DevExpress.XtraCharts.Native.Node;

    [TestFixture]
    public class DstControllerTestFixture
    {
        private DstController controller;
        private Mock<IOpcClientService> opcClient;
        private Mock<IHubController> hubController;
        private Mock<IOpcSessionHandler> opcSessionHandler;
        private Mock<IMappingEngine> mappingEngine;

        private readonly List<ReferenceDescription> referenceDescriptionCollection = new List<ReferenceDescription>
        {
            new ReferenceDescription { NodeId = ExpandedNodeId.Parse("server_methods"), BrowseName = new QualifiedName("server_methods"), NodeClass = NodeClass.Object},
            new ReferenceDescription { NodeId = ExpandedNodeId.Parse("method_run"), BrowseName = new QualifiedName("method_run"), NodeClass = NodeClass.Method},
            new ReferenceDescription { NodeId = ExpandedNodeId.Parse("method_reset"),BrowseName = new QualifiedName("method_reset"), NodeClass = NodeClass.Method},
        };

        private Mock<IStatusBarControlViewModel> statusBarViewModel;
        private Iteration iteration;
        private Assembler assembler;

        [SetUp]
        public void Setup()
        {
            this.hubController = new Mock<IHubController>();

            this.hubController.Setup(x => x.CurrentDomainOfExpertise).Returns(new DomainOfExpertise());

            var uri = new Uri("http://t.e");
            this.assembler = new Assembler(uri);

            this.iteration =
                new Iteration(Guid.NewGuid(), this.assembler.Cache, uri)
                {
                    Container = new EngineeringModel(Guid.NewGuid(), this.assembler.Cache, uri)
                    {
                        EngineeringModelSetup = new EngineeringModelSetup(Guid.NewGuid(), this.assembler.Cache, uri)
                        {
                            RequiredRdl = { new ModelReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, uri) },
                            Container = new SiteReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, uri)
                            {
                                Container = new SiteDirectory(Guid.NewGuid(), this.assembler.Cache, uri)
                            }
                        }
                    }
                };

            this.assembler.Cache.TryAdd(new CacheKey(this.iteration.Iid, null),new Lazy<Thing>(() => this.iteration));

            this.hubController.Setup(x => x.OpenIteration).Returns(this.iteration);

            this.hubController.Setup(
                    x => x.CreateOrUpdate(
                        It.IsAny<ExternalIdentifierMap>(), It.IsAny<Action<Iteration, ExternalIdentifierMap>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            this.hubController.Setup(
                x => x.CreateOrUpdate(
                    It.IsAny<IEnumerable<ElementDefinition>>(), It.IsAny<Action<Iteration, ElementDefinition>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            this.hubController.Setup(
                x => x.CreateOrUpdate(
                    It.IsAny<IEnumerable<IdCorrespondence>>(), It.IsAny<Action<ExternalIdentifierMap, IdCorrespondence>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            this.hubController.Setup(
                x => x.Delete(
                    It.IsAny<IEnumerable<IdCorrespondence>>(), It.IsAny<Action<ExternalIdentifierMap, IdCorrespondence>>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            this.hubController.Setup(x => x.Write(It.IsAny<ThingTransaction>())).Returns(Task.CompletedTask);

            this.opcSessionHandler = new Mock<IOpcSessionHandler>();

            this.mappingEngine = new Mock<IMappingEngine>();

            this.opcClient = new Mock<IOpcClientService>();
            this.opcClient.Setup(x => x.Connect(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IUserIdentity>())).Returns(Task.CompletedTask);
            this.opcClient.Setup(x => x.OpcClientStatusCode).Returns(OpcClientStatusCode.Connected);
            this.opcClient.Setup(x => x.CloseSession());
            this.opcClient.Setup(x => x.ReadNode(Variables.Server_ServerStatus_StartTime)).Returns(new DataValue(new DateTime(2021, 1, 1)));
            this.opcClient.Setup(x => x.ReadNode(Variables.Server_ServerStatus_CurrentTime)).Returns(new DataValue(new DateTime(2021, 1, 3)));
            
            this.opcClient.Setup(x => x.References).Returns(new ReferenceDescriptionCollection(new List<ReferenceDescription>()
            {
                new ReferenceDescription() { NodeId = new ExpandedNodeId(Guid.NewGuid(), 4), NodeClass = NodeClass.Variable},
                new ReferenceDescription() { NodeId = new ExpandedNodeId(Guid.NewGuid(), 2), BrowseName = new QualifiedName("dummy"), NodeClass = NodeClass.Method}
            }));

            this.statusBarViewModel = new Mock<IStatusBarControlViewModel>();
            this.statusBarViewModel.Setup(x => x.Append(It.IsAny<string>(), It.IsAny<StatusBarMessageSeverity>()));

            this.controller = new DstController(this.opcClient.Object, this.hubController.Object, this.opcSessionHandler.Object, this.mappingEngine.Object, this.statusBarViewModel.Object);
        }

        [Test]
        public void VerifyProperties()
        {
            Assert.Null(this.controller.ServerAddress);
            Assert.Zero(this.controller.RefreshInterval);
            Assert.IsTrue(this.controller.IsSessionOpen);
            Assert.IsNotEmpty(this.controller.Variables);
            Assert.IsNotNull(this.controller.References);
            Assert.IsNotEmpty(this.controller.Methods);
            Assert.AreEqual(MappingDirection.FromDstToHub, this.controller.MappingDirection);
            Assert.IsEmpty(this.controller.DstMapResult);
            Assert.IsEmpty(this.controller.IdCorrespondences);
            Assert.IsNull(this.controller.ExternalIdentifierMap);
            Assert.IsNotEmpty(this.controller.ThisToolName);
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
        public void VerifyMap()
        {
            this.opcClient.Setup(x => x.ReadNode(It.IsAny<NodeId>())).Returns(new DataValue());

            this.mappingEngine.Setup(x => x.Map(It.IsAny<object>()))
                .Returns(new List<ElementDefinition>());
            
            this.controller.ExternalIdentifierMap = new ExternalIdentifierMap()
            {
                Container = this.iteration
            };

            Assert.DoesNotThrowAsync(() => this.controller.Map(new List<VariableRowViewModel>()));

            this.mappingEngine.Setup(x => x.Map(It.IsAny<object>())).Throws<InvalidOperationException>();
            Assert.ThrowsAsync<InvalidOperationException>(() => this.controller.Map(default(List<VariableRowViewModel>)));

            this.mappingEngine.Verify(x => x.Map(It.IsAny<object>()), Times.Exactly(2));
        }
        
        [Test]
        public void VerifyTransfert()
        {
            this.controller.ExternalIdentifierMap = new ExternalIdentifierMap(Guid.NewGuid(), null, null)
            {
                Container = new ModelReferenceDataLibrary(Guid.NewGuid(), this.assembler.Cache, null)
                { 
                    Container = new SiteDirectory(Guid.NewGuid(), this.assembler.Cache, null)
                }
            };

            Assert.DoesNotThrowAsync(async() => await this.controller.TransferMappedThingsToHub());

            this.hubController.Verify(
                x => x.Write(It.IsAny<ThingTransaction>()), Times.Exactly(2));

            this.hubController.Verify(
                x => x.Refresh(), Times.Once);
        }

        [Test]
        public void VerifyUpdateExternalIdentifierMap()
        {
            const string oldCorrespondenceExternalId = "old";

            this.controller.ExternalIdentifierMap = new ExternalIdentifierMap()
            {
                Correspondence =
                {
                    new IdCorrespondence() { ExternalId = oldCorrespondenceExternalId },
                    new IdCorrespondence() { ExternalId = "-1" }
                },
                Container = this.iteration
            };

            this.controller.IdCorrespondences.AddRange(new[]
            {
                new IdCorrespondence() { ExternalId = "0"}, new IdCorrespondence() { ExternalId = "1" },
                new IdCorrespondence() { ExternalId = "-1" }
            });

            Assert.DoesNotThrowAsync(() => this.controller.UpdateExternalIdentifierMap());

            this.hubController.Verify(
                x=> x.Write(It.IsAny<ThingTransaction>()),
                Times.Once);

            this.hubController.Verify(
                x => x.Delete(
                    It.IsAny<IEnumerable<IdCorrespondence>>(), It.IsAny<Action<ExternalIdentifierMap, IdCorrespondence>>(), It.IsAny<bool>()),
                Times.Once);

            Assert.AreEqual(4, this.controller.ExternalIdentifierMap.Correspondence.Count());
            Assert.IsNotNull(this.controller.ExternalIdentifierMap.Correspondence.SingleOrDefault(x => x.ExternalId == oldCorrespondenceExternalId));
            Assert.IsNotNull(this.controller.ExternalIdentifierMap.Correspondence.SingleOrDefault(x => x.ExternalId == "-1"));
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
            this.controller.IsSessionOpen = false;
            Assert.IsNull(this.controller.GetServerStartTime());

            this.controller.IsSessionOpen = true;
            Assert.AreEqual(new DateTime(2021, 1, 1), this.controller.GetServerStartTime());
            this.opcClient.Verify(x => x.ReadNode(Variables.Server_ServerStatus_StartTime), Times.Once);
        }

        [Test]
        public void VerifyGetCurrentServerTime()
        {
            this.controller.IsSessionOpen = false;
            Assert.IsNull(this.controller.GetCurrentServerTime());

            this.controller.IsSessionOpen = true;
            Assert.AreEqual(new DateTime(2021, 1, 3), this.controller.GetCurrentServerTime());
            this.opcClient.Verify(x => x.ReadNode(Variables.Server_ServerStatus_CurrentTime), Times.Once);
        }

        [Test]
        public void VerifyCreateExternalIdentifierMap()
        {
            Assert.DoesNotThrowAsync(async () => await this.controller.CreateExternalIdentifierMap("Name"));

            this.hubController.Verify(x => x.CreateOrUpdate(
                It.IsAny<ExternalIdentifierMap>(), It.IsAny<Action<Iteration, ExternalIdentifierMap>>(), 
                It.IsAny<bool>()), Times.Once);
        }
    }
}
