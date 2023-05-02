import React from "react";
import ReactDOM from "react-dom";
import App from "./statement";

const Wrapper = () => {
	return (
		<div className="system-content">
			<div className="page-title">Transaction History</div>
			<div className="sub-page">
				<div className="sub-title active">
					<a href="deposit-history.html">Deposit history</a>
				</div>
				<div className="sub-title">
					<a href="transfer-history.html">Transfer history</a>
				</div>
				<div className="sub-title">
					<a href="withdrawal-history.html">Withdrawal history</a>
				</div>
				<div className="sub-title">
					<a href="statement.html">Statement</a>
				</div>
			</div>
			<Panel />
		</div>
	);
};

const Panel = () => <App />;

export default Wrapper;

if (process.env.NODE_ENV === "production") {
	ReactDOM.render(<Panel />, document.getElementById("content"));
}
