using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Runtime;
using GraphQL;
using GraphQL.Types;
using Newtonsoft.Json;

// http://docs.aws.amazon.com/amazondynamodb/latest/developerguide/DotNetSDKMidLevel.html
// http://docs.aws.amazon.com/amazondynamodb/latest/developerguide/ScanMidLevelDotNet.html

namespace AwsDotnetCsharp
{
    public class Handler
    {
        const string postsTableEnvVar = "DYNAMODB_POSTS_TABLE";
        const string commentsTableEnvVar = "DYNAMODB_AUTHORS_TABLE";
        const string authorsTableEnvVar = "DYNAMODB_COMMENTS_TABLE";

        AmazonDynamoDBClient dynamoDbClient { get; set; }

        Table postsTable;

        Table commentsTable;

        Table authorsTable;

        string postsTableName;
        string commentsTableName;

        string authorsTableName;

        public Handler()
        {
            // Load tables
            postsTableName = Environment.GetEnvironmentVariable(postsTableEnvVar);
            commentsTableName = Environment.GetEnvironmentVariable(commentsTableEnvVar);
            authorsTableName = Environment.GetEnvironmentVariable(authorsTableEnvVar);

            dynamoDbClient = new AmazonDynamoDBClient();

            try
            {
                postsTable = Table.LoadTable(dynamoDbClient, postsTableName);
            }
            catch (AmazonDynamoDBException e) { Console.WriteLine(e.Message); }
            catch (AmazonServiceException e) { Console.WriteLine(e.Message); }
            catch (Exception e) { Console.WriteLine(e.Message); }

            try
            {
                commentsTable = Table.LoadTable(dynamoDbClient, commentsTableName);
            }
            catch (AmazonDynamoDBException e) { Console.WriteLine(e.Message); }
            catch (AmazonServiceException e) { Console.WriteLine(e.Message); }
            catch (Exception e) { Console.WriteLine(e.Message); }

            try
            {
                authorsTable = Table.LoadTable(dynamoDbClient, authorsTableName);
            }
            catch (AmazonDynamoDBException e) { Console.WriteLine(e.Message); }
            catch (AmazonServiceException e) { Console.WriteLine(e.Message); }
            catch (Exception e) { Console.WriteLine(e.Message); }
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
        public async Task<APIGatewayProxyResponse> GraphQL(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var queryJson = (request?.Body);
            var jsonObj = JsonConvert.DeserializeObject<Dictionary<string,string>>(queryJson);

            if (jsonObj.ContainsKey("query")) {
                queryJson = (jsonObj["query"]);    
            }

            var schema = new Schema {
                Query = new BlogQuery(this),
                Mutation = new BlogMutation(this)
            };

            var result = await new DocumentExecuter().ExecuteAsync(_ => {
                _.Schema = schema;
                _.Query = queryJson;
            }).ConfigureAwait(true);
            
            var json = new GraphQL.Http.DocumentWriter(indent: true).Write(result);
            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = json,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
            return response;
        }

        #region GraphQL Types
        public class Author
        {
            public string id { get; set; }
            public string name { get; set; }
        }


        public class AuthorType : ObjectGraphType<Author>
        {
            public AuthorType()
            {
                Name = "Author";
                Description = "An Author of a post or comment";
                Field(x => x.id).Name("id").Description("Id of author");
                Field(x => x.name).Name("name").Description("Name of author");
            }
        }

        public class Comment
        {
            public string id { get; set; }
            public string content { get; set; }
            public string author { get; set; }
        }

        public class CommentType : ObjectGraphType<Comment>
        {
            public CommentType()
            {
                Name = "Comment";
                Description = "Comment of a post";

                Field(x => x.id).Name("id").Description("Id of comment");
                Field(x => x.content).Name("content").Description("Content of comment");
                Field(x => x.author).Name("author").Description("Author of comment");
            }
        }

        public class Post
        {
            public string id { get; set; }
            public string title { get; set; }

            public string content { get; set; }

            public string author { get; set; }
        }

        public class PostType : ObjectGraphType<Post>
        {

            public PostType()
            {
                Field(x => x.id).Name("id").Description("Post Id");
                Field(x => x.title).Name("title").Description("Post Title");
                Field(x => x.content).Name("content").Description("Post Body Content");
                Field(x => x.author).Name("author").Description("Author Id");
            }
        }

        #endregion

        #region GraphQL Infrastructure
        public class BlogQuery : ObjectGraphType<object>
        {
            public BlogQuery(Handler handler)
            {
                Name = "Query";

                Field<ListGraphType<PostType>>(
                "posts",
                "List of posts in the blog",
                resolve: context => handler.GetPosts());


                Field<ListGraphType<AuthorType>>(
                    "authors",
                    "List of authors",
                    resolve: context => handler.GetAuthors()
                );

                Field<AuthorType>(
                    "author",
                    "Get Author by ID",
                    arguments: new QueryArguments(
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "Id of author" }
                    ),
                    resolve: context => handler.GetAuthor(context.GetArgument<string>("id"))
                );
            }
        }

        public class BlogMutation : ObjectGraphType<object>
        {
            public BlogMutation(Handler handler)
            {
                Name = "Mutation";

                Field<PostType>(
                    "createPost",
                    "Create a post",
                    arguments: new QueryArguments(
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "title", Description = "Post Title" },
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "content", Description = "Post Body Content" },
                        new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "author", Description = "Post Author" }
                    ),
                    resolve: context =>
                    {
                        var myTitle = context.GetArgument<string>("title");
                        var myContent = context.GetArgument<string>("content");
                        var myAuthor = context.GetArgument<string>("author");

                        Post post = new Post
                        {
                            id = Guid.NewGuid().ToString(),
                            title = myTitle,
                            content = myContent,
                            author = myAuthor
                        };

                        return handler.CreatePost(post);
                    }
                );
            }
        }

        public class BlogSchema : Schema
        {
            public BlogSchema(Func<Type, GraphType> resolveType) : base(resolveType)
            {
                Query = (BlogQuery)resolveType(typeof(BlogQuery));
                Mutation = (BlogMutation)resolveType(typeof(BlogMutation));
            }
        }

        #endregion

        #region GraphQL Functions

        public async Task<Post> CreatePost(Post post)
        {
            Document document = new Document();
            document.Add("id", post.id);
            document.Add("title", post.title);
            document.Add("content", post.content);
            document.Add("author", post.author);

            Document result = null;
            try
            {
                result = await postsTable.PutItemAsync(document);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Put Item Exception. Message: "+ex.Message);
            }

            return post;
        }

        public async Task<List<Post>> GetPosts()
        {
            ScanOperationConfig scanConfig = new ScanOperationConfig
            {
                Select = SelectValues.SpecificAttributes,
                AttributesToGet = new List<string> { "id", "title", "content", "author" }

            };

            var search = postsTable.Scan(scanConfig);
            
            // get all async
            List<Document> searchResult = null;

            try {
                searchResult = await search.GetRemainingAsync();
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                throw ex;
            }
            
            var result = new List<Post>();

            foreach (Document document in searchResult)
            {
                Post post = new Post
                {
                    id = document["id"].AsString(),
                    title = document["title"].AsString(),
                    content = document["content"].AsString(),
                    author = document["author"].AsString()
                };
                result.Add(post);
            }
            return result;
        }

        public async Task<Author> GetAuthor(string id)
        {
            GetItemOperationConfig config = new GetItemOperationConfig
            {
                AttributesToGet = new List<string> { "id", "name" },
            };

            Document getResult = null;

            Author author = new Author();

            try
            {
                getResult = await authorsTable.GetItemAsync(id, config);
                author.id = getResult["id"];
                author.name = getResult["name"];
            }
            catch (Exception ex)
            {
                Console.WriteLine("Can't Get Author. Message: "+ex.Message);
            }

            return author;
        }

        public async Task<List<Author>> GetAuthors()
        {
            ScanOperationConfig scanConfig = new ScanOperationConfig
            {
                Select = SelectValues.SpecificAttributes,
                AttributesToGet = new List<string> { "id", "name" },
                ConsistentRead = true
            };

            var search = authorsTable.Scan(scanConfig);

            // get all async
            var searchResult = await search.GetRemainingAsync();

            var result = new List<Author>();
            foreach (Document document in searchResult)
            {
                Author author = new Author
                {
                    id = document["id"],
                    name = document["name"]
                };
                result.Add(author);
            }

            return result;
        }

        public async Task<List<Comment>> GetComments()
        {
            ScanOperationConfig scanConfig = new ScanOperationConfig
            {
                Select = SelectValues.SpecificAttributes,
                AttributesToGet = new List<string> { "id", "content", "author" },
                ConsistentRead = true
            };

            var search = commentsTable.Scan(scanConfig);

            // get all async
            var searchResult = await search.GetRemainingAsync();

            var result = new List<Comment>();
            foreach (Document document in searchResult)
            {
                Comment comment = new Comment
                {
                    id = document["id"],
                    content = document["content"],
                    author = document["author"]
                };
                result.Add(comment);
            }

            return result;
        }

        #endregion

    }
}
