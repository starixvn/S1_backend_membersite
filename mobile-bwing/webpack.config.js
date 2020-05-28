const path = require("path");
//const webpack = require('webpack');
//const bundleAnalyzer = require("webpack-bundle-analyzer").BundleAnalyzerPlugin;

module.exports = {
	mode: "production",
	optimization: {
		mangleWasmImports: true,
		nodeEnv: "production"
	},
	externals: {
		react: "React",
		"react-dom": "ReactDOM",
		moment: "moment",
		jquery: "$",
		axios: "axios"
	},
	entry: {
		"account/Profile.min": ["./src/profile/viewProfile.js"],
		"account/UpdProfile.min": ["./src/profile/updProfile.js"],
		"history/Deposit.min": ["./src/history/deposit.js"],
		"history/Withdrawal.min": ["./src/history/withdrawal.js"],
		"history/Transfer.min": ["./src/history/fundTransfer.js"],
		"history/Statement/Statement.min": ["./src/history/statement.js"],
		"message/personalMsg/personalMsg.min": ["./src/message/personalMsg.js"],
		"message/announcement/announcement.min": [
			"./src/message/announcement.js"
		]
	},
	output: {
		path: path.resolve(
			__dirname,
			"../MemberSite/MemberSite/wwwroot/js/Bwing/component/"
		),
		filename: "[name].js"
	},
	resolve: {
		extensions: [".js", ".jsx"]
	},
	module: {
		rules: [
			{
				test: /\.(js|jsx)$/,
				exclude: /(node_modules)/,
				use: {
					loader: "babel-loader",
					options: {
						presets: [
							[
								"@babel/preset-env",
								{
									corejs: "3",
									useBuiltIns: "entry"
								}
							],
							"@babel/preset-react",
							{
								plugins: [
									"@babel/plugin-proposal-class-properties"
								]
							}
						]
					}
				}
			},
			{
				test: /\.css$/,
				use: ["style-loader", "css-loader"]
			}
		]
	}
};
