import React, { useState, useRef, useEffect } from "react";
/* eslint-disable */

const defCountDownSec = 300;

export default props => {
	return (
		<div className="modal" data-open={props.isOpen}>
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

const IllegalPhone = ({ errMsg, ClickClose, errCode }) => {
	const [count, setCount] = useState(defCountDownSec);
	const refCount = useRef(count);
	const isMountedRef = useRef(null);
	refCount.current = count;

	const showTimer = errCode === 1004;

	useEffect(() => {
		isMountedRef.current = true;
		if (showTimer) {
			Timer();
		}
		return () => {
			isMountedRef.current = false;
		};
	}, [showTimer]);

	const Timer = () => {
		if (isMountedRef.current) {
			let newCount = refCount.current - 1;
			setCount(newCount);
			if (newCount !== 0) {
				setTimeout(Timer, 1000);
			}
		}
	};

	return (
		<>
			<div className="modal__body">
				<p className="txt-lh">
					{errMsg}
					{showTimer ? ` ( ${count} ) ` : null}
				</p>
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
	const [count, setCount] = useState(defCountDownSec);
	const refCount = useRef(count);
	const isMountedRef = useRef(null);
	refCount.current = count;

	useEffect(() => {
		isMountedRef.current = true;
		Timer();
		return () => {
			isMountedRef.current = false;
		};
	}, []);

	const Timer = () => {
		if (isMountedRef.current) {
			let newCount = refCount.current - 1;
			setCount(newCount);
			if (newCount !== 0) {
				setTimeout(Timer, 1000);
			}
		}
	};

	const onResend = () => {
		if (refCount.current === 0) {
			setCount(defCountDownSec);
			ClickResend();
		}
	};

	let canResend = count === 0;

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
						className={
							canResend ? "btn btn-primary" : "btn btn-cancel"
						}
						type="button"
						disabled={!canResend}
						onClick={onResend}>
						<span className="text">
							{i18n.lbl_Resend}
							{canResend ? null : ` ( ${count} ) `}
						</span>
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
