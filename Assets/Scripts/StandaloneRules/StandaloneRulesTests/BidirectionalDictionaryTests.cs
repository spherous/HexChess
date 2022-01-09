using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BidirectionalDictionaryTests
{
    [Test]
    public void StartsEmpty()
    {
        var dict = new BidirectionalDictionary<char, int>();
        Assert.AreEqual(0, dict.Count);
    }
    [Test]
    public void SimpleLookup()
    {
        var dict = new BidirectionalDictionary<char, int>();
        dict.Add('A', 1);

        Assert.True(dict.ContainsKey('A'));
        Assert.True(dict.ContainsKey(1));

        Assert.False(dict.ContainsKey('B'));
        Assert.False(dict.ContainsKey(2));

        Assert.AreEqual('A', dict[1]);
        Assert.AreEqual(1, dict['A']);

        Assert.Throws<KeyNotFoundException>(() =>
        {
            var _ = dict['B'];
        });
    }

    [Test]
    public void AddDuplicateChecking()
    {
        var dict = new BidirectionalDictionary<char, int>();
        dict.Add('A', 1);

        Assert.Throws<InvalidOperationException>(() =>
        {
            dict.Add('A', 1);
        });
        Assert.AreEqual(1, dict.Count);
        Assert.AreEqual(1, dict['A']);
        Assert.AreEqual('A', dict[1]);

        Assert.Throws<InvalidOperationException>(() =>
        {
            dict.Add('A', 2);
        });
        Assert.AreEqual(1, dict.Count);
        Assert.AreEqual(1, dict['A']);
        Assert.AreEqual('A', dict[1]);

        Assert.Throws<InvalidOperationException>(() =>
        {
            dict.Add('B', 1);
        });
        Assert.AreEqual(1, dict.Count);
        Assert.AreEqual(1, dict['A']);
        Assert.AreEqual('A', dict[1]);
    }
    [Test]
    public void IndexAssignDuplicateChecking()
    {
        var dict = new BidirectionalDictionary<char, int>();
        dict.Add('A', 1);

        dict['A'] = 2;
        Assert.AreEqual(2, dict['A']);
        Assert.AreEqual('A', dict[2]);

        // TODO: The old 1 should be removed
        Assert.False(dict.ContainsKey(1));

        Assert.AreEqual(1, dict.Count);
    }

    [Test]
    public void RemoveTest()
    {
        var dict = new BidirectionalDictionary<char, int>();
        dict.Add('A', 1);
        dict.Add('B', 2);

        Assert.False(dict.Remove('C'));
        Assert.AreEqual(2, dict.Count);

        Assert.False(dict.Remove(3));
        Assert.AreEqual(2, dict.Count);

        Assert.True(dict.Remove(2));
        Assert.AreEqual(1, dict.Count);

        Assert.False(dict.Remove('B'));
        Assert.AreEqual(1, dict.Count);

        Assert.True(dict.Remove('A'));
        Assert.AreEqual(0, dict.Count);

        Assert.False(dict.Remove(1));
        Assert.AreEqual(0, dict.Count);
    }
}
