edge-fs
=======

This is a F# compiler for edge.js.

See [edge.js overview](http://tjanczuk.github.com/edge) and [edge.js on GitHub](https://github.com/tjanczuk/egde) for more information. 

###Quick install guide

It should be pretty easy to get going on Windows: (From memory:)

1. Make sure you have F# installed with your Visual Studio 2012
2. ```git clone git@github.com:7sharp9/edge-fs.git```
3. ```cd edge-fs```
4. I tested the npm install locally with ```npm install . -g```
5. Navigate to a test project
6. Install edge-fs into your test project with npm install <edge-fs folder>
7. create a test node.js file along the lines of:  

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
