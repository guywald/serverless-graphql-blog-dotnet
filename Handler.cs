using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

using GraphQL;
using GraphQL.Types;

// http://docs.aws.amazon.com/amazondynamodb/latest/developerguide/DotNetSDKMidLevel.html

namespace AwsDotnetCsharp
{
    public class Handler
    {
        const string postsTableEnvVar = "DYNAMODB_POSTS_TABLE";
        const string commentsTableEnvVar = "DYNAMODB_AUTHORS_TABLE";
        const string authorsTableEnvVar = "DYNAMODB_COMMENTS_TABLE";

        AmazonDynamoDBClient dynamoDbClient { get; set; }

        public Handler()
        {
            // Load tables
            var postsTaleName = Environment.GetEnvironmentVariable(postsTableEnvVar);
            var comFmentsTableName = Environment.GetEnvironmentVariable(commentsTableEnvVar);
            var authorsTableName = Environment.GetEnvironmentVariable(authorsTableEnvVar);

            dynamoDbClient = new AmazonDynamoDBClient();

            
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
        public async Task<APIGatewayProxyResponse> GraphQL(APIGatewayProxyRequest request, ILambdaContext context)
        {
            await Task.Delay(0);

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = "{}",
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
            return response;
        }

        #region GraphQL Types
        public class Author
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }


        public class AuthorType : ObjectGraphType<Author>
        {
            public AuthorType(Handler handler)
            {
                Name = "Author";
                Description = "An Author of a post or comment";
                Field(x => x.Id).Description("Id of author");
                Field(x => x.Name).Description("Name of author");
            }
        }

        public class Comment
        {
            public string Id { get; set; }
            public string Content { get; set; }
            public Author Author { get; set; }
        }

        public class CommentType : ObjectGraphType<Comment>
        {
            public CommentType(Handler handler)
            {
                Name = "Comment";
                Description = "Comment of a post";

                Field(x => x.Id).Description("Id of comment");
                Field(x => x.Content).Description("Content of comment");
                Field<AuthorType>("author", "Author of comment", resolve: context=> handler.GetAuthor(context.Source.Author.Id));
            }
        }

        public class Post
        {
            public string Id { get; set; }
            public string Title { get; set; }

            public string BodyContent { get; set; }

            public Author Author { get; set; }
        }

        public class PostType : ObjectGraphType<Post>
        {

            public PostType(Handler handler)
            {
                Field(x => x.Id).Description("Post Id");
                Field(x => x.Title).Description("Post Title");
                Field(x => x.BodyContent).Description("Post Body Content");

                Field<AuthorType>("Author", "Author of post", resolve: context=>handler.GetAuthor(context.Source.Id));
            }
        }

        #endregion

        #region GraphQL Infrastructure
        public class BlogQuery : ObjectGraphType<object>
        {
            public BlogQuery()
            {
                Name = "Query";


            }


        }

        public class BlogSchema : Schema
        {
            public BlogSchema(Func<Type, GraphType> resolveType) : base(resolveType)
            {
                Query = (BlogQuery)resolveType(typeof(BlogQuery));
            }
        }

        #endregion

        #region GraphQL Functions

        public async Task<bool> CreatePost(Post post)
        {
            await Task.Delay(0);
            throw new System.NotImplementedException();
        }

        public async Task<Post> GetPosts()
        {
            await Task.Delay(0);
            throw new System.NotImplementedException();
        }

        public async Task<Author> GetAuthor(string id)
        {
            await Task.Delay(0);
            throw new System.NotImplementedException();
        }

        public async Task<Author[]> GetAuthors()
        {
            await Task.Delay(0);
            throw new System.NotImplementedException();
        }

        public async Task<Comment[]> GetComments()
        {
            await Task.Delay(0);
            throw new System.NotImplementedException();
        }

        #endregion

    }
}
