var path = require('path');
var webpack = require('webpack');
//const UglifyJSPlugin = require('uglifyjs-webpack-plugin');

module.exports = {
  entry:{
		"js/component/home/registerForm.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/home/RegisterForm.jsx'
    ],
    "js/component/account/Profile.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/account/Profile.jsx'
		],
    "js/component/app/loginForm.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/app/LoginForm.jsx'
		],
    "js/commJS/account.min":[
			'../MemberSite/MemberSite/wwwroot/js/commJS/Account'
    ],
    "js/component/app/BalancePanel.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/app/BalancePanel.jsx'
		],
    "js/component/cashier/DepositPanel.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/cashier/DepositPanel.jsx'
		],
    "js/component/account/ChangePwd.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/account/ChangePwd.jsx'
		],
    "js/component/account/CustBankInfo.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/account/CustBankInfo.jsx'
		],
    "js/component/cashier/WithdrawalPanel.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/cashier/WithdrawalPanel.jsx'
		],
    "js/component/history/Deposit.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/history/Deposit.jsx'
		],
    "js/component/history/Withdrawal.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/history/Withdrawal.jsx'
		],
    "js/component/account/Messages.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/account/Messages.jsx'
		],
    "js/component/account/UploadPhotoId.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/account/UploadPhotoId.jsx'
		],
    "js/component/app/WalletInfo.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/app/WalletInfo.jsx'
		],
    "js/component/home/ForgetPassowrd.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/home/ForgetPassowrd.js'
		],
    "js/component/Promotion/promotions.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/Promotion/promotions.jsx'
		],
    "js/component/cashier/TransferPanel.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/cashier/TransferPanel.jsx'
		],
    "js/component/history/Transfer.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/history/Transfer.jsx'
		]},
  output: {
    path:  path.resolve(__dirname,'../MemberSite/MemberSite/wwwroot/'),
    filename: "[name].js",
	chunkFilename: "[name].chunk.js?v=" + new Date().getTime(),
	publicPath: ''
  },
  module: {
    rules : [
      {
        test: /\.(js|jsx)$/,
        loader: 'babel-loader',
        query: {
            "presets": [
                "@babel/preset-env",
                "@babel/preset-react"
            ],
            plugins: [
                [
                  "@babel/plugin-proposal-class-properties"
                ]
            ]
        }
      }
    ]
  }
};
