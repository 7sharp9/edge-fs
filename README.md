edge-fs
=======

This is a F# compiler for edge.js.

See [edge.js overview](http://tjanczuk.github.com/edge) and [edge.js on GitHub](https://github.com/tjanczuk/egde) for more information. 

###Quick install guide

It should be pretty easy to get going on Windows: (From memory:)

1. Make sure you have F# installed with your Visual Studio 2012
2. Navigate to a test project
3. Install edge.js with `npm install edgejs -g` *(Note you dont have to do a global package install, just omit the -g)*
4. Install edge-fs into your test project with `npm install git://github.com/7sharp9/edge-fs.git -g`
5. create a test node.js file along the lines of:  

   ```javascript
   var edge = require('edge');

   var helloWorld = edge.func('fs', 'fun input -> async{return ".NET welcomes " + input.ToString()}');

   helloWorld('JavaScript', function (error, result) {
       if (error) throw error;
       console.log(result);
   });
```
8. Test with ```node testfile.js```

Ill be writing more documentation over time, as well as deploying a live npm package.  
