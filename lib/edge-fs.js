exports.getCompiler = function () {
	return process.env.EDGE_FS_NATIVE || (__dirname + '\\edge-fs.dll');
};