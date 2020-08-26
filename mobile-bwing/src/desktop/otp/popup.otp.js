import React from "react";
/* eslint-disable */
export default props => {
	return (
		<div className="modal" data-open="false" id="OTPPopupContent">
			<div className="overlay"></div>
			<div className="modal__dialog">
				<button
					className="remodal-close"
					onClick={props.ClickClose}></button>
				<div className="modal__header">
					<span className="text">{i18n.lbl_OTPCertification}</span>
				</div>
				{props.isLoading ? <Loading /> : <ContentSwipper {...props} />}
			</div>
		</div>
	);
};

const ContentSwipper = props => {
	if (props.errCode === 0) {
		return <Normal {...props} />;
	} else {
		return <IllegalPhone {...props} />;
	}
};

const IllegalPhone = ({ errMsg, ClickClose }) => {
	return (
		<>
			<div className="modal__body">
				<p className="txt-lh">{errMsg}</p>
			</div>
			<div className="modal__footer">
				<div className="btn-group">
					<button
						className="btn btn-primary"
						type="button"
						onClick={ClickClose}>
						<span className="text">OK</span>
					</button>
				</div>
			</div>
		</>
	);
};

const Normal = ({ ClickVerify, ClickResend, OnChangeCode, code, errMsg }) => {
	return (
		<>
			<div className="modal__body">
				<p className="txt-lh">{i18n.msg_OTPSendCode}</p>
				<p className="txt-lh">{i18n.msg_OTPEnterCode}</p>
				<>
					<div className="form-group otpInput">
						<div className="input-field">
							<input
								className="input-control"
								type="number"
								pattern="[0-9]*"
								value={code}
								onChange={OnChangeCode}
							/>
						</div>
						<button
							className="btn btn-secondary"
							type="button"
							onClick={ClickVerify}>
							<span className="text">{i18n.lbl_Submit}</span>
						</button>
					</div>
					{errMsg ? <div className="tip">{errMsg}</div> : null}
				</>
			</div>
			<div className="modal__footer">
				<div className="btn-group">
					<button
						className="btn btn-primary"
						type="button"
						onClick={ClickResend}>
						<span className="text">{i18n.lbl_Resend}</span>
					</button>
				</div>
			</div>
		</>
	);
};

const Loading = () => {
	return (
		<div className="otpLoading">
			<i className="icon icon-refresh-white"></i>
		</div>
	);
};
/* eslint-enable */
