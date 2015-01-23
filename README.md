edge-fs
=======

[![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/7sharp9/edge-fs?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This is a F# compiler for edge.js.

See [edge.js overview](http://tjanczuk.github.com/edge) and [edge.js on GitHub](https://github.com/tjanczuk/egde) for more information. 

###Quick install guide

It should be pretty easy to get going on Windows: 

1. Make sure you have F# installed with Visual Studio 2012  
2. Navigate to a test project  
3. Install edge.js with `npm install edgejs -g` *(Note you dont have to do a global package install, just omit the -g)* 
4. Install edge-fs into your test project with `npm install edge-fs -g`  
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

Ill be writing more documentation over time.   
