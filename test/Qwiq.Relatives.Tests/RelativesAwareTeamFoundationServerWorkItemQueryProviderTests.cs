using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Qwiq.Core.Tests;
using Should;
using Microsoft.Qwiq.Linq;
using Microsoft.Qwiq.Linq.Visitors;
using Microsoft.Qwiq.Mapper;
using Microsoft.Qwiq.Mapper.Attributes;
using Microsoft.Qwiq.Mocks;
using Microsoft.Qwiq.Relatives.Linq;
using Microsoft.Qwiq.Relatives.Mapper;
using Microsoft.Qwiq.Relatives.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Qwiq.Relatives.Tests
{
    public abstract class RelativesAwareTeamFoundationServerWorkItemQueryProviderContextSpecification : ContextSpecification
    {
        protected IEnumerable<IWorkItem> WorkItemStoreWorkItems;
        protected IEnumerable<IWorkItemLinkInfo> WorkItemStoreWorkItemLinks;
        protected Query<SimpleMockModel> Query;
        protected IEnumerable<KeyValuePair<SimpleMockModel, IEnumerable<SimpleMockModel>>> Actual;

        public override void Given()
        {
            var workItemStore = new MockWorkItemStore(WorkItemStoreWorkItems, WorkItemStoreWorkItemLinks);
            var fieldMapper = new CachingFieldMapper(new FieldMapper());
            var propertyReflector = new PropertyReflector();
            var propertyInspector = new PropertyInspector(propertyReflector);
            var builder = new WiqlQueryBuilder(new RelativesAwareWiqlTranslator(fieldMapper), new PartialEvaluator(), new RelativesAwareQueryRewriter());
            var mapperStrategies = new IWorkItemMapperStrategy[]
            {
                new AttributeMapperStrategy(propertyInspector,
                    new TypeParser()),
                    new WorkItemLinksMapperStrategy(propertyInspector, workItemStore),
                    new ParentIdMapperStrategy(workItemStore)
            };
            var mapper = new WorkItemMapper(mapperStrategies);
            var queryProvider = new RelativesAwareTeamFoundationServerWorkItemQueryProvider(workItemStore, builder, mapper, fieldMapper);

            Query = new Query<SimpleMockModel>(queryProvider, builder);
        }
    }

    public abstract class QueryReturnsResults : RelativesAwareTeamFoundationServerWorkItemQueryProviderContextSpecification
    {
        public override void Given()
        {
            WorkItemStoreWorkItems = new List<IWorkItem>
            {
                new MockWorkItem("SimpleMockWorkItem", new Dictionary<string, object>
                    {
                        {"ID", 1},
                        {"Priority", 2}
                    }),
                new MockWorkItem("SimpleMockWorkItem", new Dictionary<string, object>
                    {
                        {"ID", 2},
                        {"Priority", 4}
                    }),
                new MockWorkItem("SimpleMockWorkItem", new Dictionary<string, object>
                    {
                        {"ID", 3},
                        {"Priority", 3}
                    }),
                new MockWorkItem("SimpleMockWorkItem", new Dictionary<string, object>
                    {
                        {"ID", 4},
                        {"Priority", 4}
                    }),
                new MockWorkItem("SimpleMockWorkItem", new Dictionary<string, object>
                    {
                        {"ID", 5},
                        {"Priority", 5}
                    })
            };

            WorkItemStoreWorkItemLinks = new[] {
                new MockWorkItemLinkInfo
                {
                    SourceId = 0,
                    TargetId = 3
                },
                new MockWorkItemLinkInfo
                {
                    SourceId = 3,
                    TargetId = 1
                },
                new MockWorkItemLinkInfo
                {
                    SourceId = 3,
                    TargetId = 2
                },
                new MockWorkItemLinkInfo
                {
                    SourceId = 0,
                    TargetId = 4
                },
                new MockWorkItemLinkInfo
                {
                    SourceId = 0,
                    TargetId = 5
                }
            };

            base.Given();
        }
    }

    public abstract class QueryDoesNotReturnResults : RelativesAwareTeamFoundationServerWorkItemQueryProviderContextSpecification
    {
        public override void Given()
        {
            WorkItemStoreWorkItems = Enumerable.Empty<IWorkItem>();
            WorkItemStoreWorkItemLinks = Enumerable.Empty<IWorkItemLinkInfo>();
            base.Given();
        }
    }

    [TestClass]
    public class given_a_query_provider_when_a_parents_clause_is_used : QueryReturnsResults
    {
        public override void When()
        {
            Actual = Query.Parents<SimpleMockModel, SimpleMockModel>();
        }

        [TestMethod]
        public void item_with_id_of_1_has_a_parent_of_id_3()
        {
            Actual.Single(kvp => kvp.Key.Id == 3).Value.Count(wi => wi.Id == 1).ShouldEqual(1);
        }

        [TestMethod]
        public void item_with_id_of_5_has_no_parent()
        {
            Actual.Single(kvp => kvp.Key.Id == 5).Value.ShouldBeEmpty();
        }
    }

    [TestClass]
    public class given_a_query_provider_when_a_children_clause_is_used : QueryReturnsResults
    {
        public override void When()
        {
            Actual = Query.Children<SimpleMockModel, SimpleMockModel>();
        }

        [TestMethod]
        public void item_with_id_of_3_has_a_child_of_id_2()
        {
            Actual.Single(kvp => kvp.Key.Id == 3).Value.Count(wi => wi.Id == 2).ShouldEqual(1);
        }

        [TestMethod]
        public void item_with_id_of_5_has_no_children()
        {
            Actual.Single(kvp => kvp.Key.Id == 5).Value.ShouldBeEmpty();
        }
    }


    [TestClass]
    public class when_a_parents_clause_is_used_on_a_query_with_no_results : QueryDoesNotReturnResults
    {
        public override void When()
        {
            Actual = Query.Parents<SimpleMockModel, SimpleMockModel>();
        }

        [TestMethod]
        public void an_empty_set_is_returned()
        {
            Actual.ShouldBeEmpty();
        }
    }

    [TestClass]
    public class when_a_children_clause_is_used_on_a_query_with_no_results : QueryDoesNotReturnResults
    {
        public override void When()
        {
            Actual = Query.Children<SimpleMockModel, SimpleMockModel>();
        }

        [TestMethod]
        public void an_empty_set_is_returned()
        {
            Actual.ShouldBeEmpty();
        }
    }

    [TestClass]
    public class given_a_query_provider_when_a_relatives_clause_is_used_with_a_type_with_no_workitemtype : QueryReturnsResults
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void children_clause_causes_InvalidOperationException()
        {
            var result = Query.Children<SimpleMockModel, MockModelNoType>().ToList();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void parents_clause_causes_InvalidOperationException()
        {
            var result = Query.Parents<MockModelNoType, SimpleMockModel>().ToList();
        }
    }

    [TestClass]
    public class given_a_query_provider_when_a_relatives_clause_is_used_with_a_type_with_multiple_workitemtypes : QueryReturnsResults
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void children_clause_causes_InvalidOperationException()
        {
            var result = Query.Children<SimpleMockModel, MockModelMultipleTypes>().ToList();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void parents_clause_causes_InvalidOperationException()
        {
            var result = Query.Parents<MockModelMultipleTypes, SimpleMockModel>().ToList();
        }
    }
}

