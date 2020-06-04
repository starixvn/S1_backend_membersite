import React, { useEffect, useState } from "react";
import ReactDOM from "react-dom";
import WebAPI from "../../service/api/base";
import areaCode from "../../common/areaCode";

/* eslint-disable */
const ViewProfile = () => {
	const [custInfo, setCustInfo] = useState({
		userName: "",
		accountName: "",
		email: "",
		phoneNo: "",
		areaCode: "",
		birth_d: "",
		birth_m: "",
		birth_y: "",
		countryCode: ""
	});

	useEffect(() => {
		WebAPI()
			.post("/api/account/GetCustInfoProfile")
			.then(
				res => {
					let result = res.data;
					if (result.errorCode === 0) {
						setCustInfo(result.data);
					} else {
						alert(result.message);
					}
				},
				err => {
					alert(err);
				}
			);
	}, []);
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
								{custInfo.userName}
							</div>

							<div className="label">
								{i18n["lbl_RealName"]}:{" "}
							</div>
							<div className="text-green">
								{custInfo.accountName}
							</div>

							<div className="label">{i18n["lbl_Email"]}: </div>
							<div className="text-green">{custInfo.email}</div>

							<div className="label">
								{i18n["lbl_Birthday"]}:{" "}
							</div>
							<div className="text-green">
								{birthday_format(
									custInfo.birth_d,
									custInfo.birth_m,
									custInfo.birth_y
								)}
							</div>

							<div className="label">{i18n["lbl_Phone"]}: </div>
							<div className="text-green">
								<span>
									{areaCode[custInfo.areaCode]}{" "}
									{custInfo.phoneNo}
								</span>
							</div>

							<div className="label">{i18n["lbl_Country"]}: </div>
							<div className="text-green">
								{GetDefaultCountry(custInfo.countryCode)}
							</div>
						</div>
					</div>
				</div>
			</div>
		</>
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
