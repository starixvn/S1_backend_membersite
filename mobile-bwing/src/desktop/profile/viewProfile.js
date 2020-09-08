import React, { useEffect, useState, Component } from "react";
import ReactDOM from "react-dom";
import WebAPI from "../../service/api/base";
import areaCode from "../../common/areaCode";
import OTP from "../otp/otp";

/* eslint-disable */

class ViewProfile extends Component {
	constructor(props) {
		super(props);
		this.state = {
			custInfo: {
				userName: "",
				accountName: "",
				email: "",
				phoneNo: "",
				areaCode: "",
				birth_d: "",
				birth_m: "",
				birth_y: "",
				countryCode: "",
				ShowOTP: false
			}
		};
	}

	componentDidMount() {
		WebAPI()
			.post("/api/account/GetCustInfoProfile")
			.then(
				res => {
					let result = res.data;
					if (result.errorCode === 0) {
						this.setState({ custInfo: result.data });
					} else {
						alert(result.message);
					}
				},
				err => {
					alert(err);
				}
			);
	}

	HandleShowOTP = isOpen => {
		this.setState({ ShowOTP: isOpen });
	};

	render() {
		return (
			<>
				<div className="title">{i18n.lbl_Profile}</div>
				<div className="content">
					<div id="form-info">
						<div className="form-horizontal">
							<div className="form-group">
								<div className="label">
									{i18n["lbl_Username"]}:{" "}
								</div>
								<div className="text-green">
									{this.state.custInfo.userName}
								</div>

								<div className="label">
									{i18n["lbl_RealName"]}:{" "}
								</div>
								<div className="text-green">
									{this.state.custInfo.accountName}
								</div>

								<div className="label">
									{i18n["lbl_Email"]}:{" "}
								</div>
								<div className="text-green">
									{this.state.custInfo.email}
								</div>

								<div className="label">
									{i18n["lbl_Birthday"]}:{" "}
								</div>
								<div className="text-green">
									{birthday_format(
										this.state.custInfo.birth_d,
										this.state.custInfo.birth_m,
										this.state.custInfo.birth_y
									)}
								</div>

								<div className="label">
									{i18n["lbl_Phone"]}:{" "}
								</div>
								<div className="text-green otpImg">
									<span>
										{areaCode[this.state.custInfo.areaCode]}{" "}
										{this.state.custInfo.phoneNo}
									</span>
									<OTPICON
										isVerify={
											this.state.custInfo.phoneVerified
										}
										HandleShowOTP={this.HandleShowOTP}
									/>
								</div>

								<div className="label">
									{i18n["lbl_Country"]}:{" "}
								</div>
								<div className="text-green">
									{GetDefaultCountry(
										this.state.custInfo.countryCode
									)}
								</div>
							</div>
						</div>
					</div>
				</div>

				{this.state.ShowOTP ? (
					<OTP
						isOpen={this.state.ShowOTP}
						HandleShowOTP={this.HandleShowOTP}
					/>
				) : null}
			</>
		);
	}
}

const OTPICON = ({ isVerify, HandleShowOTP }) => {
	return isVerify ? (
		<div className="icon-otp"></div>
	) : (
		<button className="otp-btn" onClick={HandleShowOTP.bind(this, true)}>
			<span className="icon-otp"></span>
			<span>Mobile verification</span>
		</button>
	);
};

const birthday_format = (d, m, y) => {
	return d === null || d === "" ? "- / - / -" : `${d}/${m}/${y}`;
};

const GetDefaultCountry = v => {
	return Country.find(x => x.Code === v)?.CountryName;
};

/* eslint-enable */

export default ViewProfile;

if (process.env.NODE_ENV === "production") {
	ReactDOM.render(<ViewProfile />, document.getElementById("content"));
}
