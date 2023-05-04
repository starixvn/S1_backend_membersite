var path = require("path");
var webpack = require("webpack");
//const UglifyJSPlugin = require('uglifyjs-webpack-plugin');

module.exports = {
	mode: "production",
	externals: {
		react: "React",
		"react-dom": "ReactDOM"
	},
	entry: {
		// "js/component/home/registerForm.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/home/RegisterForm.jsx'
		// ],
		// "js/component/account/Profile.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/account/Profile.jsx'
		// ],
		// "js/component/account/Profile.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/account/Profile/NormalProfile/index.js'
		// ],
		"js/component/account/UpdProfile.min": [
			"../MemberSite/MemberSite/wwwroot/js/component/account/Profile/UpdProfile/index.js"
		]
		// "js/component/app/loginForm.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/app/LoginForm.jsx'
		// ],
		// "js/commJS/account.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/commJS/Account'
		// ],
		// "js/component/app/BalancePanel.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/app/BalancePanel.jsx'
		// ],
		// "js/component/cashier/DepositPanel.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/cashier/DepositPanel.jsx'
		// ],
		// "js/component/account/ChangePwd.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/account/ChangePwd/ChangePwd.jsx'
		// ],
		// "js/component/account/ResetPwd.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/account/ResetPwd.jsx'
		// ],
		// "js/component/account/CustBankInfo.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/account/CustBankInfo.jsx'
		// ],
		// "js/component/cashier/WithdrawalPanel.min": [
		// 	"../MemberSite/MemberSite/wwwroot/js/component/cashier/WithdrawalPanel.jsx"
		// ]
		// "js/component/history/Deposit.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/history/Deposit.jsx'
		// ],
		// "js/component/history/Withdrawal.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/history/Withdrawal.jsx'
		// ],
		// "js/component/message/personalMsg/personalMsg.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/message/personalMsg/Messages.jsx'
		// ],
		// "js/component/message/announcement/announcement.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/message/announcement/announcement.js'
		// ],
		// "js/component/message/announcement/marquee.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/message/announcement/marquee.js'
		// ],
		// "js/component/account/UploadPhotoId.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/account/UploadPhotoId.jsx'
		// ],
		// "js/component/app/WalletInfo.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/app/WalletInfo.jsx'
		// ],
		// "js/component/home/ForgetPassowrd.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/home/ForgetPassowrd.js'
		// ],
		// "js/component/Promotion/promotions.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/Promotion/promotions.jsx'
		// ],
		// "js/component/cashier/TransferPanel.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/cashier/TransferPanel.jsx'
		// ],
		// "js/component/history/Transfer.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/history/Transfer.jsx'
		// ],
		// "js/component/account/ChangePwdAlert.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/account/ChangePwd/ChangePwdAlert/index.js'
		// ],
		// "js/component/casino/lobby.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/casino/lobby/index.js'
		// ],
		// "js/component/history/Statement/Statement.min":[
		// 	'../MemberSite/MemberSite/wwwroot/js/component/history/Statement/Statement.js'
		// ]
	},
	output: {
		path: path.resolve(__dirname, "../MemberSite/MemberSite/wwwroot/"),
		filename: "[name].js",
		chunkFilename: "[name].chunk.js?v=" + new Date().getTime(),
		publicPath: ""
	},
	module: {
		rules: [
			{
				test: /\.(js|jsx)$/,
				loader: "babel-loader",
				query: {
					presets: ["@babel/preset-env", "@babel/preset-react"],
					plugins: [["@babel/plugin-proposal-class-properties"]]
				}
			}
		]
	}
};
