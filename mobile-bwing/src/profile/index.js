import React, { useState } from "react";
import ReactDOM from "react-dom";
import UpdProfile from "./updProfile";
import ViewProfile from "./viewProfile";

const Wrapper = () => {
	return (
		<div className="system-content">
			<div className="page-title">My Account</div>
			<div className="sub-page">
				<div className="sub-title">
					<a href="change-password.html">Change Password</a>
				</div>
				<div className="sub-title active">
					<a href="profile.html">Profile</a>
				</div>
				<div className="sub-title">
					<a href="bank-info.html">Bank Info</a>
				</div>
			</div>
			<Panel />
		</div>
	);
};

const Panel = () => <UpdProfile />;

export default Wrapper;

if (process.env.NODE_ENV === "production") {
	ReactDOM.render(<Panel />, document.getElementById("content"));
}
