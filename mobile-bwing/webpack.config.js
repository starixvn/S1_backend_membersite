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
		// "account/UpdProfile.min": ["./src/profile/updProfile.js"],
		// "history/Deposit.min": ["./src/history/deposit.js"],
		// "history/Withdrawal.min": ["./src/history/withdrawal.js"],
		// "history/Transfer.min": ["./src/history/fundTransfer.js"],
		// "history/Statement/Statement.min": ["./src/history/statement.js"],
		// "message/personalMsg/personalMsg.min": ["./src/message/personalMsg.js"],
		// "message/announcement/announcement.min": [
		// 	"./src/message/announcement.js"
		// ],
		// "cashier/TransferPanel.min": ["./src/cashier/transfer.js"],
		// "cashier/WithdrawalPanel.min": ["./src/cashier/withdrawal.js"],
		// "cashier/DepositPanel.min": ["./src/cashier/deposit.js"],
		// "desktop/history/Deposit.min": ["./src/desktop/history/deposit.js"],
		// "desktop/history/Withdrawal.min": [
		// 	"./src/desktop/history/withdrawal.js"
		// ],
		// "desktop/history/Transfer.min": [
		// 	"./src/desktop/history/fundTransfer.js"
		// ],
		// "desktop/history/Statement/Statement.min": [
		// 	"./src/desktop/history/statement.js"
		// ],
		"desktop/account/Profile.min": ["./src/desktop/profile/viewProfile.js"]
		// "desktop/account/UpdProfile.min": [
		// 	"./src/desktop/profile/updProfile.js"
		// ],
		// "desktop/cashier/WithdrawalPanel.min": [
		// 	"./src/desktop/cashier/withdrawal.js"
		// ],
		// "desktop/cashier/DepositPanel.min": [
		// 	"./src/desktop/cashier/deposit.js"
		// ],
		// "desktop/message/personalMsg/personalMsg.min": [
		// 	"./src/desktop/message/personalMsg.js"
		// ],
		// "desktop/message/announcement/announcement.min": [
		// 	"./src/desktop/message/announcement.js"
		// ],
		// "desktop/cashier/TransferPanel.min": [
		// 	"./src/desktop/cashier/transfer.js"
		// ],
		// "desktop/message/announcement/marquee.min": [
		// 	"./src/desktop/marquee.js"
		// ],
		// "../../../SiteSkin/Bwing/Desktop/js/after.min": [
		// 	"@babel/polyfill",
		// 	"../MemberSite/MemberSite/wwwroot/SiteSkin/Bwing/Desktop/js/after.js"
		// ],
		// "../../../SiteSkin/Bwing/Mobile/js/after.min": [
		// 	"@babel/polyfill",
		// 	"../MemberSite/MemberSite/wwwroot/SiteSkin/Bwing/Mobile/js/after.js"
		// ]
		// "../../../SiteSkin/Bwing/Desktop/js/before.min": [
		// 	"../MemberSite/MemberSite/wwwroot/SiteSkin/Bwing/Mobile/js/before.js"
		// ],
		// "desktop/account/ResetPwd.min": [
		// 	"../MemberSite/MemberSite/wwwroot/js/Bwing/component/desktop/account/ResetPwd.jsx"
		// ]
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
			},
			{
				test: /\.(png|jpe?g|gif)$/i,
				use: [
					{
						loader: "file-loader"
					}
				]
			}
		]
	}
};
