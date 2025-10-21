using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Models.Comparers;
using NetTally.Models.Creation;
using NetTally.Models.Defaults;
using NetTally.Models.Posts;

namespace NetTally.Tests.Components.Posts;

[TestClass]
public class PostTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    [TestMethod]
    public void Construct_NullOrigin_Error()
    {
        var post = Post.Create(null!, "Some text");
        Assert.IsNull(post);
    }

    [TestMethod]
    public void Construct_NullText_Error()
    {
        var origin = Origin.None;
        var post = Post.Create(origin, null!);
        Assert.IsNull(post);
    }

    [TestMethod]
    public void Compare_Same()
    {
        var author1 = Author.Create("Kinematics");
        Assert.IsNotNull(author1);
        var origin1 = Origin.CreateUserNameOnly(author1);
        Assert.IsNotNull(origin1);
        var post1 = Post.Create(origin1, "Some text");
        var post2 = Post.Create(origin1, "Some text");
        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);

        Assert.AreEqual(post1, post2, PostComparer.Instance);
    }

    [TestMethod]
    public void Compare_DifferentText()
    {
        var origin = Origin.None;
        var post1 = Post.Create(origin, "Some text");
        var post2 = Post.Create(origin, "Some more text");
        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);
        Assert.AreNotEqual(post1, post2);
    }

    [TestMethod]
    public void Compare_DifferentOrigins()
    {
        var author1 = Author.Create("Kinematics");
        var author2 = Author.Create("Cammy");
        Assert.IsNotNull(author1);
        Assert.IsNotNull(author2);

        var origin1 = Origin.CreateUserNameOnly(author1);
        var origin2 = Origin.CreateUserNameOnly(author2);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        var post1 = Post.Create(origin1, "Some text");
        var post2 = Post.Create(origin2, "Some text");
        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);

        Assert.AreNotEqual(post1, post2);
    }

    [TestMethod]
    public void Compare_DifferentOrigins2_Same()
    {
        var author1 = Author.Create("Kinematics");
        var author2 = Author.Create("Kinematics");
        Assert.IsNotNull(author1);
        Assert.IsNotNull(author2);

        var origin1 = Origin.CreateUser(author1,
            new Uri("https://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/"),
            new Uri("https://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/post-2236809"),
            PostId.Create(123456),
            PostNumber.Create(150),
            DateTimeOffset.MinValue);
        var origin2 = Origin.CreateUserNameOnly(author2);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreEqual(origin1, origin2, OriginComparer.Instance);

        var post1 = Post.Create(origin1, "Some text");
        var post2 = Post.Create(origin2, "Some text");
        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);

        Assert.AreEqual(post1, post2, PostComparer.Instance);
    }
}
