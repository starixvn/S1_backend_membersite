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
    "js/component/home/loginForm.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/home/LoginForm.jsx'
		],
    "js/commJS/account.min":[
			'../MemberSite/MemberSite/wwwroot/js/commJS/Account'
    ],
    "js/component/account/BalancePanel.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/account/BalancePanel.jsx'
		],
    "js/component/cashier/DepositPanel.min":[
			'../MemberSite/MemberSite/wwwroot/js/component/cashier/DepositPanel.jsx'
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
